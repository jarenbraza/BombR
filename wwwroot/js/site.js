// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Adds "is-invalid" class to mark the specified element as invalid.
function markAsInvalid(elementId) {
    document.getElementById(elementId).classList.add("is-invalid");
}

// Removes "is-invalid" class to unmark the specified element as invalid.
function unmarkAsInvalid(elementId) {
    document.getElementById(elementId).classList.remove("is-invalid");
}

// Checks if the input is empty.
// Assumes the element specified by the element ID is an input.
function isInputEmpty(elementId) {
    return document.getElementById(elementId).value.length == 0;
}

// Raises error through both a window alert and the console.
function handleError(err) {
    alert(err.toString());
    return console.error(err.toString());
}

// Creates a cookie with key-value of a specified name, value, and expiration date.
// If the cookie already exists, it is overwritten with the new values.
function createCookie(name, value, days) {
    const expiration = getExpirationString(days);
    document.cookie = name + "=" + value + expiration + "; path=/";
}

// Returns the value of a cookie if it exists, or null otherwise.
function readCookie(name) {
    const nameEquals = name + "=";
    const cookieArray = document.cookie.split(';');

    for (let i = 0; i < cookieArray.length; i++) {
        let currentCookie = cookieArray[i].trim();

        if (currentCookie.indexOf(nameEquals) == 0) {
            let cookieValue = currentCookie.substring(nameEquals.length, currentCookie.length);
            return cookieValue;
        }
    }

    return null;
}

// Erases a cookie by overwriting it with a cookie that does not have a value.
function eraseCookie(name) {
    createCookie(name, "", -1);
}

// Returns whether the browser is able to serve cookies, yum!
// Note that it does not check for cookie configuration types or maximum expiration dates.
function areCookiesEnabled() {
    let result = false;
    const testCookieName = "testCookieName-A67ZG";
    const testCookieValue = "testValue";

    createCookie(testCookieName, testCookieValue, 1);

    if (readCookie(testCookieName) != null) {
        eraseCookie(testCookieName);
        result = true;
    }

    return result;
}

// Returns an expiration string formatted to be used for cookie creation.
// This string is created based on the current date and a specified days to expiration.
function getExpirationString(daysToExpiration) {
    let expiration = "";

    if (daysToExpiration) {
        const date = new Date();
        date.setDate(date.getDate() + daysToExpiration);
        expiration = "; expires=" + date.toGMTString();
    }

    return expiration;
}

// Returns the value associated with the specified key within the URL parameters.
function getQueryValue(key) {
    const parameters = location.search.substring(1);
    const queryStrings = parameters.split("&");

    // Take account of space characters to be encoded in query strings.
    key = key.replace(" ", "%20");

    for (let i = 0; i < queryStrings.length; i++) {
        const keyValuePair = queryStrings[i].split("=");

        if (keyValuePair[0] == key) {
            return keyValuePair[1];
        }
    }

    return null;
}

function getRoomNameFromPath(path) {
    return path.substring(path.lastIndexOf("/") + 1, path.length);
}

// Returns if a key is alphanumeric.
function isAlphaNumeric(key) {
    if (key.length != 1) {
        return false;
    }

    const charCode = key.charCodeAt(0);

    return (charCode > 47 && charCode < 58) ||  // Numeric (0-9)
           (charCode > 64 && charCode < 91) ||  // Upper alphabetical (A-Z)
           (charCode > 96 && charCode < 123)    // Lower alphabetical (a-z)
};

// Returns if a key is used by the game.
function isGameKeyCode(key) {
    return (key === "Space") ||
           (key === "KeyW") ||
           (key === "KeyA") ||
           (key === "KeyS") ||
           (key === "KeyD") ||
           (key === "ArrowUp") ||
           (key === "ArrowLeft") ||
           (key === "ArrowDown") ||
           (key === "ArrowRight");
}