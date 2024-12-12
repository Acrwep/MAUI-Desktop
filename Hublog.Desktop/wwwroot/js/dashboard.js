function openPunchoutConfirmationModal() {
    const modalElement = new bootstrap.Modal(document.getElementById('punchoutConfirmationModal'), {
        backdrop: 'static',
        keyboard: true
    });
    modalElement.show();
}
function closePunchoutConfirmationModal() {
    var modalElement = document.getElementById('punchoutConfirmationModal');
    if (modalElement) {
        var modal = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
        if (modal) {
            modal.hide();
        }

        // Ensure backdrop is removed
        const backdrops = document.querySelectorAll('.modal-backdrop');
        backdrops.forEach(backdrop => backdrop.remove());

        // Remove "modal-open" class from body
        document.body.classList.remove('modal-open');
    } else {
        console.error("Modal element not found");
    }
}

function openBreakTimerModal() {
    console.log("openBreakModal called");
    const modalElement = new bootstrap.Modal(document.getElementById('breakTimerModal'), {
        backdrop: 'static',
        keyboard: true
    });
    modalElement.show();
}

function openBreakModal() {
    console.log("openBreakModal called");
    const modalElement = new bootstrap.Modal(document.getElementById('breakModal'), {
        backdrop: 'static',
        keyboard: true
    });
    modalElement.show();
}

function closeBreakModal() {
    var modal = bootstrap.Modal.getInstance(document.getElementById('breakModal'));
    modal.hide();
}

function closeBreakTimerModal() {
    var modal = bootstrap.Modal.getInstance(document.getElementById('breakTimerModal'));
    modal.hide();
}

function changeResumeButtonColorToRed() {
    var button = document.querySelector('.breakResume_button');
    if (button) {
        button.style.backgroundColor = 'red';
        button.style.color = 'white';
    }
}

function resetResumeButtonColor() {
    var button = document.querySelector('.breakResume_button');
    if (button) {
        button.style.backgroundColor = ''; 
        button.style.color = ''; 
    }
}

function openInactiveModal() {
    const modalElement = new bootstrap.Modal(document.getElementById('inactiveModal'), {
        backdrop: 'static',
        keyboard: true
    });
    modalElement.show();
}
function closeInactiveModal() {
    var modalElement = document.getElementById('inactiveModal');
    if (modalElement) {
        var modal = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
        if (modal) {
            modal.hide();
        }

        // Ensure backdrop is removed
        const backdrops = document.querySelectorAll('.modal-backdrop');
        backdrops.forEach(backdrop => backdrop.remove());

        // Remove "modal-open" class from body
        document.body.classList.remove('modal-open');
    } else {
        console.error("Modal element not found");
    }
}

function openLogoutModal() {
    const modalElement = new bootstrap.Modal(document.getElementById('logoutModal'), {
        backdrop: 'static',
        keyboard: true
    });
    modalElement.show();
}
function closeLogoutModal() {
    var modalElement = document.getElementById('logoutModal');
    if (modalElement) {
        var modal = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
        if (modal) {
            modal.hide();
        }

        // Ensure backdrop is removed
        const backdrops = document.querySelectorAll('.modal-backdrop');
        backdrops.forEach(backdrop => backdrop.remove());

        // Remove "modal-open" class from body
        document.body.classList.remove('modal-open');
    } else {
        console.error("Modal element not found");
    }
}
function openNetworkModal() {
    const modalElement = new bootstrap.Modal(document.getElementById('networkModal'), {
        backdrop: 'static',
        keyboard: true
    });
    modalElement.show();
}
function closeNetworkModal() {
    var modalElement = document.getElementById('networkModal');
    if (modalElement) {
        var modal = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
        if (modal) {
            modal.hide();
        }

        // Ensure backdrop is removed
        const backdrops = document.querySelectorAll('.modal-backdrop');
        backdrops.forEach(backdrop => backdrop.remove());

        // Remove "modal-open" class from body
        document.body.classList.remove('modal-open');
    } else {
        console.error("Modal element not found");
    }
}

function openErrorModal() {
    const modalElement = new bootstrap.Modal(document.getElementById('errorModal'), {
        backdrop: 'static',
        keyboard: true
    });
    modalElement.show();
}
function closeErrorModal() {
    var modalElement = document.getElementById('errorModal');
    if (modalElement) {
        var modal = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
        if (modal) {
            modal.hide();
        }

        // Ensure backdrop is removed
        const backdrops = document.querySelectorAll('.modal-backdrop');
        backdrops.forEach(backdrop => backdrop.remove());

        // Remove "modal-open" class from body
        document.body.classList.remove('modal-open');
    } else {
        console.error("Modal element not found");
    }
}


function playAudio() {
    var audioElement = document.getElementById("audioPlayer");
    if (audioElement) {
        audioElement.play();
    }
};

function pauseAudio() {
    var audioElement = document.getElementById("audioPlayer");
    if (audioElement) {
        audioElement.pause();
        audioElement.currentTime = 0; // Reset the audio to the start
    }
};