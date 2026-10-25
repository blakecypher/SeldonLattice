# Security

## Reporting a vulnerability

Please report suspected vulnerabilities privately through GitHub's **Report a vulnerability**
feature on this repository (Security → Advisories → Report a vulnerability). Do not open a public
issue for an unfixed security problem.

Include the affected commit, a minimal reproduction, and what you expected verification to
establish. Reports about the limits described below are still welcome, but please read this document
first: several of them are deliberate scope boundaries rather than bugs.

## What this repository is

This is the storage and verification core of a larger system. It is published so the content
addressing, Merkle linking, signing, and verification logic can be reviewed independently. The API,
LLM orchestration, and frontend are not in scope here.

## Security properties

The design provides three distinct properties, and it is important not to conflate them.

### 1. Integrity of stored bytes

A node's identifier is the SHA3-256 hash of its serialized bytes. Verification recomputes it and
compares it with the key the node is filed under. Any modification, substitution, or unreadable
payload is reported.

This property holds against an attacker with write access to the store, because it is checked from
the bytes themselves. It does **not** depend on signatures.

### 2. Signature validity

Each node can carry an ML-DSA-65 signature over the same bytes. Verification checks that the
signature is present (under strict policy), bound to this node, and valid for these bytes. The
signature carries the public key, so no key lookup is needed.

This property catches tampering with individually signed nodes. On its own it does **not** establish
who produced a chain: an attacker who can rewrite the store can re-sign the rewritten chain with
their own key, and every signature will verify.

### 3. Authenticity relative to an anchor and pinned keys

Two out-of-band inputs close the gap:

- **A session root** recorded where the attacker cannot change it. `SessionRoot.Compute` is
  domain-separated, order-independent, and deduplicated, so it can be recomputed from any complete
  copy of a session and compared with the recorded value. Any addition, removal, or replacement
  changes the root.
- **A set of trusted signer public keys** in `VerificationPolicy.TrustedSignerPublicKeys`. A valid
  signature from a key outside the set fails with `signature.untrusted-key`.

Without both of these, verification can prove internal consistency but not authorship or history.
The default policy has no pinned keys; pinning is opt-in.

## Threat model

In scope:

- Accidental corruption of the store file.
- An attacker with filesystem write access who modifies, deletes, substitutes, or reorders nodes.
- An attacker who copies a signature from one node to another.
- An attacker who replaces the whole store and re-signs it with a different key, when the caller has
  recorded a root or pinned signer keys.

Out of scope:

- An attacker who controls the anchor as well as the store.
- An attacker who controls the signer's private key or seed file.
- Rollback to an older, validly signed state when the caller has no newer anchor.
- Traffic analysis or metadata leakage (everything is stored in plaintext).
- Denial of service by deleting or corrupting the store; corruption is *detected*, not prevented.
- Multi-writer consistency; the store is process-local.

## Operational guidance

- Record session roots outside the store. A root in the same directory as the store provides no
  protection. Useful anchors include signed Git commits, build artefacts reproduced elsewhere, an
  RFC 3161 timestamp authority, or a transparency log.
- Pin signer public keys for any deployment where authorship matters.
- Keep ML-DSA seed files out of version control and off shared volumes. `MlDsaSigner` restricts
  permissions to owner read/write on POSIX systems on a best-effort basis, but it is not a key
  management system.
- Use a KMS or HSM for production signing. This repository's signer is a reference implementation.
- Treat `VerificationPolicy.Permissive` as a migration tool, not a production policy.

## Known limitations

These are deliberate scope boundaries of the published core:

1. **No trusted time.** Node and signature timestamps are self-asserted.
2. **No revocation.** A compromised signer key can still produce valid signatures; removing it from
   the pinned set is the only mitigation, and it is retroactive for all sessions.
3. **No key rotation.** Pinning a set supports multiple keys, but there is no rotation protocol or
   key-transition evidence.
4. **Legacy parsing paths.** Pre-type-wrapper blobs and `sha256`-prefixed CIDs remain readable for
   migration. They are compatibility paths, not part of the security boundary.
5. **Anchor freshness.** A root proves the session matches that root; it does not prove the root is
   the most recent one.
6. **Single-process storage.** `FasterMerkleStore` is not a distributed database and does not defend
   against concurrent writers outside the process.

## Supported versions

This is a single-branch public subset. Security fixes are applied to `main`; there are no maintained
release branches.
