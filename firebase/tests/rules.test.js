// Firestore security-rules tests for HANGARoo (issue #31, Story 2.2).
//
// Covers the two guarantees the schema makes:
//   1. positive  - a signed-out client can READ word content;
//   2. negative  - no client can WRITE content (signed-out or signed-in),
//                  and a user can only write their own users/{userId} doc.
//
// Runs entirely against the Firestore emulator via scripts/test-firestore-rules.sh.
// No credentials are involved; the tests never touch a real project.

const { initializeTestEnvironment, assertFails, assertSucceeds } =
  require("@firebase/rules-unit-testing");
const { readFileSync } = require("fs");
const { doc, getDoc, setDoc } = require("firebase/firestore");

const PROJECT_ID = "demo-hangaroo";

describe("firestore security rules", () => {
  let env;

  before(async () => {
    env = await initializeTestEnvironment({
      projectId: PROJECT_ID,
      firestore: {
        rules: readFileSync("firebase/firestore.rules", "utf8"),
      },
    });
  });

  after(async () => env.cleanup());

  beforeEach(async () => {
    await env.clearFirestore();
    await env.withSecurityRulesDisabled(async (ctx) => {
      const db = ctx.firestore();
      await setDoc(doc(db, "words/gravity-medium"), {
        answer: "GRAVITY",
        clue: "Natural Force",
        hint: "7 letters, starts with G and ends with Y",
        categoryId: "science",
        difficultyId: "medium",
        weight: 1,
        isActive: true,
        schemaVersion: 1,
      });
      await setDoc(doc(db, "users/alice"), { bestScore: 120, totalRuns: 4 });
    });
  });

  it("content is readable while signed out", async () => {
    const db = env.unauthenticatedContext().firestore();
    await assertSucceeds(getDoc(doc(db, "words/gravity-medium")));
  });

  it("content is NOT writable while signed out", async () => {
    const db = env.unauthenticatedContext().firestore();
    await assertFails(
      setDoc(doc(db, "words/hacked-easy"), { answer: "HACKED", isActive: true })
    );
  });

  it("content is NOT writable even by a signed-in user", async () => {
    const db = env.authenticatedContext("mallory").firestore();
    await assertFails(setDoc(doc(db, "words/gravity-medium"), { answer: "PWNED" }));
    await assertFails(setDoc(doc(db, "contentMeta/singleton"), { contentVersion: 999 }));
  });

  it("a user can read and write their own progress document", async () => {
    const db = env.authenticatedContext("alice").firestore();
    await assertSucceeds(getDoc(doc(db, "users/alice")));
    await assertSucceeds(setDoc(doc(db, "users/alice"), { bestScore: 150 }, { merge: true }));
  });

  it("a user can NOT read or write another user's document", async () => {
    const db = env.authenticatedContext("mallory").firestore();
    await assertFails(getDoc(doc(db, "users/alice")));
    await assertFails(setDoc(doc(db, "users/alice"), { bestScore: 0 }));
  });

  it("an anonymous (signed-out) client can NOT write progress", async () => {
    const db = env.unauthenticatedContext().firestore();
    await assertFails(setDoc(doc(db, "users/nobody"), { bestScore: 1 }));
  });
});
