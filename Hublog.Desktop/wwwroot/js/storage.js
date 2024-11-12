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