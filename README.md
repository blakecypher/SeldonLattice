# Seldon Lattice

A content-addressed reasoning store for .NET, with tamper-evident verification of everything written to it.

Every node that enters the store gets a content ID computed from its own bytes. Change the bytes and you get a different ID. That property is what makes the store auditable: a whole session can be re-verified offline, and any alteration, deletion, or substitution shows up as a failure.

This repository is the public subset of a larger private system. It publishes the storage and verification core and its tests. The API, the LLM orchestration layer, and the web frontend are not published.

## What is here

| Project | Contents |
|---|---|
| `src/SeldonLattice.Domain` | Domain model: content IDs, Merkle nodes, sessions, state manifests, agent signatures, vector utilities. 102 C# files. |
| `src/SeldonLattice.Infrastructure` | `Storage/FasterMerkleStore.cs` (708 lines) and `Verification/` (`SessionVerifier`, `ExportVerifier`, `ExportNode`). |
| `tests/SeldonLattice.Domain.Tests` | 10 test files, 54 tests. |

Roughly 7,000 lines of C# across the three projects.

## The parts worth reading

**Content addressing.** A node's content ID is computed from its serialized bytes, never stored alongside them. Any change to a node therefore changes its ID, which is what makes later comparison meaningful.

**Tamper-evident verification.** `SessionVerifier` walks a session's nodes and distinguishes three classes of failure: a signature that no longer verifies (the content was mutated), a reference that resolves to nothing (the content was deleted), and a node whose content no longer matches its recorded ID (the content was substituted). Because a broken reference is detectable, the same mechanism doubles as revocation.

**Post-quantum signing.** `AgentSignature` carries an ML-DSA signature (NIST standard) together with the public key, so a signature is self-verifying without a lookup. The public key is stored with the signature; the private key never leaves the signer.

**Vector comparison.** `Domain/Utilities/SimdVectorUtility` is a SIMD-accelerated cosine similarity used for embedding comparison.

## Domain vocabulary

The domain model is expressed in the vocabulary of a reasoning engine: dialectical nodes, debate turns, dialogue quality state, rhetorical moves. That naming reflects what the engine does, which is run multiple LLM agents through structured argument rounds and record the reasoning as auditable data. The models themselves are ordinary serializable records.

## Build and test

Requires the .NET 10 SDK.

```bash
dotnet test
```

54 tests, 0 failures.

## Scope and limitations

This is a subset, published so that the storage and verification core can be reviewed on its own. The full system adds an ASP.NET Core API, a multi-agent LLM orchestration layer on Semantic Kernel, and a React/TypeScript frontend. Those parts are private and are not represented here.

The published core is the part that is self-contained and reusable. It has no dependency on the LLM layer: the store and the verifiers operate on content bytes and signatures, not on model calls.

## Licence

MIT. See [LICENSE](LICENSE).
