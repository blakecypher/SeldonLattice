# Seldon Lattice

A content-addressed reasoning store for .NET, with tamper-evident verification of everything written to it.

Every node that enters the store gets a content ID computed from its own bytes. Change the bytes and
the ID changes. That property is what makes the store auditable: a whole session can be re-verified
later, and alteration, deletion, or substitution shows up as a failure rather than as a slightly
different history.

This repository is the public, self-contained subset of a larger private system. It publishes the
storage and verification core, its tests, and a runnable demonstration. The API, the LLM
orchestration layer, and the web frontend are not included.

> **Status.** The wider project is paused and published here as a reviewable core. This README
> describes what the code in this repository actually does, including what it cannot do on its own.

## What is here

| Project | Contents |
|---|---|
| `src/SeldonLattice.Domain` | Domain model: content IDs, Merkle nodes, sessions, manifests, agent signatures, vector utilities. ~4,800 lines of C#. |
| `src/SeldonLattice.Infrastructure` | `FasterMerkleStore` persistence, ML-DSA signing, `SessionVerifier`, `ExportVerifier`, session roots. ~1,700 lines. |
| `tests/SeldonLattice.Domain.Tests` | 102 tests, including the regression tests for the failures described below. ~2,300 lines. |
| `samples/VerificationDemo` | A console app that tampers with a real store and prints what the verifier does. |

Roughly 9,000 lines of C# in total. Target framework: .NET 10.

## Run it

Requires the .NET 10 SDK.

```bash
dotnet build
dotnet run --project samples/VerificationDemo
```

The demo creates a temporary store, signs a three-node session, and then attacks it across five
scenarios: editing a signed leaf, deleting a node, replacing the store with different content,
and re-signing the same content with a different key. It prints each verification report and
exits non-zero if any expectation does not hold.

The test suite uses xUnit v3's in-process runner:

```bash
dotnet run --project tests/SeldonLattice.Domain.Tests
```

`dotnet test` also works on a standard SDK installation; the `dotnet run` form is the one CI uses
because it needs no external test host.

## What verification checks

`SessionVerifier` reads a session from the store and checks every node four ways:

1. **Content addressing.** The identifier the node is filed under must still hash to the node's
   current bytes.
2. **Reference integrity.** Every parent, citation, support, attack, and audit link must resolve to
   a node that exists in the store.
3. **Signature integrity.** A signature must be present (per policy), bound to this exact node, and
   cryptographically valid for these bytes. When the policy pins signer keys, the signing key must
   also be in the pinned set.
4. **Session root.** When the caller supplies an expected root, the root recomputed from the
   verified node identifiers must match it.

`ExportVerifier` applies the same checks offline to an exported node list. Each exported node carries
its original serialized bytes and signature, so no store access is needed. Exports are ordered by
content ID, so two exports of the same session are byte-identical.

Findings carry stable machine-readable codes:

| Code | Severity | Meaning |
|---|---|---|
| `content.id-mismatch` | error | Bytes no longer hash to the identifier they are filed under. |
| `content.unreadable` | error | A stored payload cannot be decoded, so its content is gone. |
| `link.dangling` | error | A cross-reference does not resolve. |
| `signature.missing` | error (strict) / warning (permissive) | No companion signature. |
| `signature.bound-to-other-node` | error | A valid signature is filed against a different node. |
| `signature.invalid` | error | The signature does not verify against the bytes. |
| `signature.untrusted-key` | error | The signature verifies, but its key is not pinned by policy. |
| `root.mismatch` | error | The recomputed session root differs from the anchored root. |
| `session.empty` | warning | No nodes were found for the session. |

`VerificationPolicy.Strict` is the default: unsigned nodes are errors.
`VerificationPolicy.Permissive` downgrades a missing signature to a warning and is intended only for
legacy or partially signed stores.

## Threat model

The store is self-consistent by design. That is enough to catch accidental corruption and partial
tampering, but not enough to establish authenticity: an attacker with write access can replace every
node and sign the replacement with their own key.

| Attacker capability | Caught without an anchor? | Caught with an anchor / pinned keys? |
|---|---|---|
| Edit a node's bytes, leave the key | Yes (`content.id-mismatch`, `signature.invalid`) | Yes |
| Delete a referenced node | Yes (`link.dangling`) | Yes |
| Corrupt a payload | Yes (`content.unreadable`) | Yes |
| Copy a signature onto another node | Yes (`signature.bound-to-other-node`) | Yes |
| Add, remove, or replace nodes and re-sign with a new key | No — the result is internally consistent | Yes (`root.mismatch`, `signature.untrusted-key`) |
| Re-sign identical content with another key | No — identical content gives identical IDs and root | Yes (`signature.untrusted-key`) |

### Anchors

A root only has evidential value if it is recorded somewhere the attacker does not control.
`SessionRoot.Compute` produces a domain-separated, order-independent, deduplicated digest over the
session's node identifiers. Record it outside the store — a build artefact, a signed commit, an
RFC 3161 timestamp, or a transparency log — and pass it back when verifying:

```csharp
var anchoredRoot = SessionRoot.Compute(nodeCids); // recorded out of band at write time
var report = await verifier.VerifySessionAsync(sessionId, anchoredRoot);
```

### Pinned signer keys

A signature carries its own public key, so it proves only that the bytes and the key agree. To turn
that into "a known agent produced these bytes", pin the accepted keys:

```csharp
var policy = new VerificationPolicy
{
    TrustedSignerPublicKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        Convert.ToBase64String(expectedPublicKey),
    },
};

var verifier = new SessionVerifier(store, signer, logger, policy);
```

With an empty set (the default) any self-consistent key is accepted. With a populated set, a chain
re-signed by another key fails with `signature.untrusted-key`.

## Design notes

**Storage.** `FasterMerkleStore` is an in-memory dictionary with append-only persistence to
`store.dat` (one base64 line per key), plus periodic compaction. Keys are prefixed by kind
(`obj:`, `sig:`, `head:`), and stored blobs carry a whitelisted type name so reads cannot
instantiate arbitrary types.

**Identifiers.** `Cid` is `sha3-256-` plus a Base64URL hash over the node's MessagePack bytes.
Node serialization is byte-stable across a round trip; a test pins that property because everything
downstream depends on it.

**Signatures.** `MlDsaSigner` is a reference `IPqcSigner` implementation backed by BouncyCastle's
managed ML-DSA-65. Signing is deterministic for a given key and content, and keys are derived from a
32-byte seed stored per signer identity (or held in memory when no key directory is configured).
BouncyCastle is used instead of `System.Security.Cryptography.MLDsa` because the platform
implementation is not available on every target.

**Why the store scan changed.** `IMerkleStore.ScanBlobsAsync<T>` returns each node together with the
key it is filed under, plus the keys that failed to decode. The older `GetAllNodesAsync<T>` projection
discarded both facts, which is why a rewritten node and a destroyed node could previously verify
clean. `GetAllNodesAsync<T>` is retained for compatibility and now delegates to the scan.

## Known limitations

This is a verification core, not a complete secure system. In particular:

- **No key management.** Seed files are only as safe as the filesystem. Production use needs a KMS or
  HSM, key rotation, and revocation.
- **No trusted time.** Signatures and node timestamps are self-asserted. Use an external timestamp
  authority if time matters.
- **No confidentiality.** Everything is stored in the clear.
- **Anchors are the caller's responsibility.** Verification cannot tell you that a root is the right
  root; it can only tell you whether the store still matches the root you supplied.
- **Pinning is opt-in.** The default policy accepts any self-consistent key.
- **Single-process storage.** `FasterMerkleStore` is process-local and append-only with compaction;
  it is not a distributed or multi-writer database.
- **Legacy formats are accepted.** The reader still understands pre-type-wrapper blobs and
  `sha256`-prefixed CIDs for migration. Those paths are compatibility, not a security boundary.

See [SECURITY.md](SECURITY.md) for the threat model in more detail and for reporting guidance.

## Licence

MIT. See [LICENSE](LICENSE).
