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
        modal.hide();
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
        modal.hide();
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
        modal.hide();
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