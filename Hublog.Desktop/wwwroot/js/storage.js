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
