"use strict";

var edge = require("edge"),
    fs = require("fs"),
    os = require("os"),
    fluid = require("infusion");

var windowRestore = fluid.registerNamespace("windowRestore");

var queryWindows = edge.func({
    source: __dirname + "/windowRestore.csx",
    typeName: "WindowRestore",
    methodName: "Query"
});

var restoreWindows = edge.func({
    source: __dirname + "/windowRestore.csx",
    typeName: "WindowRestore",
    methodName: "Restore"
});

var result = queryWindows(null, true);

console.log("GOT RESULT: " + JSON.stringify(result, null, 2));

var trial = [
    {
        "title": "Fairlight CMI - Wikipedia - Google Chrome",
        "left": 12,
        "top": 9,
        "right": 650,
        "bottom": 411,
        "hwnd": 1707710
    },
    {
        "title": "things - Google Search - Google Chrome",
        "left": 405,
        "top": 158,
        "right": 2323,
        "bottom": 2012,
        "hwnd": 2431466
    }
];

windowRestore.mkdir = function (path) {
    fluid.log(fluid.logLevel.WARN, "Creating GPII settings directory in ", path);
    try { // See code skeleton in http://stackoverflow.com/questions/13696148/node-js-create-folder-or-use-existing
        fs.mkdirSync(path);
    } catch (e) {
        if (e.code !== "EEXIST") {
            fluid.fail("Unable to create snapshots directory, code is " + e.code + ": exception ", e);
        }
    }
};

/** Formats a millisecond timestamp into an ISO-8601 date string which is safe to
  * appear in a filename
  * @param {Number} A millisecond timestamp, as dispensed from `Date.now()`
  * @return {String} An ISO-8601 date stamp without colon characters, e.g. 2016-07-05T220712.549Z
  */
// Taken from gpii.journal.formatTimestamp
windowRestore.formatTimestamp = function (time) {
    var date = new Date(time);
    var stamp = date.toISOString();
    var safeStamp = stamp.replace(/:/g, ""); // This is still a valid ISO-8601 date string, but Date.parse() will no longer parse it
    return safeStamp;
};

windowRestore.snapshotDir = os.tmpdir() + "/windowSnapshots/";


if (process.argv[2] === "restore") {
    restoreWindows(trial, true);
} else {
    var snapshot = JSON.stringify(result, null, 2);
    windowRestore.mkdir(windowRestore.snapshotDir);
    var snapshotFile = windowRestore.snapshotDir + "snapshot-" + windowRestore.formatTimestamp(Date.now()) + ".json";
    fs.writeFileSync(snapshotFile, snapshot);
}
