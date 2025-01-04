function setItem(key, value) {
    localStorage.setItem(key, value);
}

function getItem(key) {
    return localStorage.getItem(key);
}

function removeItem(key) {
    localStorage.removeItem(key);
}

function clearStorage() {
    localStorage.clear();
}
function getToken() {
    return localStorage.getItem('loginToken');
}
function getuserDetails() {
    return localStorage.getItem('userDetails');
}
function getelapsedTime() {
    return localStorage.getItem('elapsedTime' || "00:00:00");
}
function getpunchInTime() {
    return localStorage.getItem('punchInTime' || null);
}
function getAppCloseTime() {
    return localStorage.getItem('appCloseTime' || null);
}
function getbreakStatus() {
    return localStorage.getItem('breakStatus' || null);
}
function getbreakAlertStatus() {
    return localStorage.getItem('breakAlertStatus' || null);
}
function getactiveBreakId() {
    return localStorage.getItem('activeBreakId' || null);
}
function getTriggerInactiveAlert() {
    return localStorage.getItem('triggerInactiveAlert' || null);
}
function getInactivityAlertStatus() {
    return localStorage.getItem('inactivityAlertStatus' || null);
}
function getLastsynctime() {
    return localStorage.getItem('lastsyncTime' || null);
}
//audio
function getResumeworkAudioStatus() {
    return localStorage.getItem('resumeWorkAudioStatus' || null);
}