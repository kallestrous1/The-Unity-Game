const fs = require("fs");
const path = require("path");

const LOG_PATH = path.join(__dirname, "events.log");

function saveEvents(events) {
    const lines = events
        .map(evt => JSON.stringify(evt))
        .join("\n") + "\n";

    fs.appendFileSync(LOG_PATH, lines);
}

function loadEvents() {
    console.log("sending persisted events to database");
    if (!fs.existsSync(LOG_PATH)) {
        return [];
    }

    const data = fs.readFileSync(LOG_PATH, "utf-8").trim();
    if (!data) return [];

    return data
        .split("\n")
        .map(line => JSON.parse(line));
}

module.exports = {
    saveEvents,
    loadEvents
};