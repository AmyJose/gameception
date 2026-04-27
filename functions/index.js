const {onCall, HttpsError} = require("firebase-functions/v2/https");
const admin = require("firebase-admin");

admin.initializeApp({
  databaseURL: "https://groove-galaxy-6d5c7-default-rtdb.europe-west1.firebasedatabase.app/",
});

exports.submitScore = onCall(async (request) => {
  const auth = request.auth;
  const data = request.data;

  if (!auth) {
    throw new HttpsError("unauthenticated", "User must be signed in.");
  }

  if (!data) {
    throw new HttpsError("invalid-argument", "Missing request data.");
  }

  const name = typeof data.name === "string" ? data.name.trim() : "Player";
  const safeName = name.length > 20 ? name.slice(0, 20) : name || "Player";

  const score = Number(data.score);
  const promptsHit = Number(data.promptsHit);
  const promptsMissed = Number(data.promptsMissed);
  const longestStreak = Number(data.longestStreak);
  const sequencesCompleted = Number(data.sequencesCompleted);
  const accuracy = Number(data.accuracy);
  const runDuration = Number(data.runDuration);

  if (!Number.isFinite(score) || score < 0) {
    throw new HttpsError("invalid-argument", "Invalid score.");
  }

  if (!Number.isFinite(promptsHit) || promptsHit < 0) {
    throw new HttpsError("invalid-argument", "Invalid promptsHit.");
  }

  if (!Number.isFinite(promptsMissed) || promptsMissed < 0) {
    throw new HttpsError("invalid-argument", "Invalid promptsMissed.");
  }

  if (!Number.isFinite(longestStreak) || longestStreak < 0) {
    throw new HttpsError("invalid-argument", "Invalid longestStreak.");
  }

  if (!Number.isFinite(sequencesCompleted) || sequencesCompleted < 0) {
    throw new HttpsError("invalid-argument", "Invalid sequencesCompleted.");
  }

  if (!Number.isFinite(accuracy) || accuracy < 0 || accuracy > 1) {
    throw new HttpsError("invalid-argument", "Invalid accuracy.");
  }

  if (!Number.isFinite(runDuration) || runDuration < 0) {
    throw new HttpsError("invalid-argument", "Invalid runDuration.");
  }

  const entry = {
    uid: auth.uid,
    name: safeName,
    score,
    promptsHit,
    promptsMissed,
    longestStreak,
    sequencesCompleted,
    accuracy,
    runDuration,
    timestamp: Date.now(),
  };

  const ref = admin.database().ref("leaderboard/scores").push();
  console.log("Writing leaderboard entry:", entry);
  try {
    await ref.set(entry);
    console.log("Score written:", ref.key);
  } catch (error) {
    console.error("Database write failed:", error);
    throw new HttpsError("internal", "Database write failed.");
  }

  return {
    ok: true,
    id: ref.key,
  };
});
