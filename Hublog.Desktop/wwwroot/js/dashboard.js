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
            modal.hide();
            modal.hide();

            const backdrop = document.querySelector('.modal-backdrop');
            if (backdrop) {
                backdrop.remove();
            }
            document.body.classList.remove('modal-open');

            setTimeout(() => {
                if (!document.querySelector('.modal.show')) {
                    document.body.classList.remove('modal-open');
                    let backdrops = document.querySelectorAll('.modal-backdrop');
                    backdrops.forEach(backdrop => backdrop.remove());
                }
            }, 500);
        }
    } else {
        console.error("Modal element not found");
    }
}

function openShiftModal() {
    const modalElement = new bootstrap.Modal(document.getElementById('shiftModal'), {
        backdrop: 'static',
        keyboard: true
    });
    modalElement.show();
}
function closeShiftModal() {
    var modalElement = document.getElementById('shiftModal');
    if (modalElement) {
        var modal = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
        if (modal) {
            modal.hide();
            modal.hide();
            modal.hide();

            const backdrop = document.querySelector('.modal-backdrop');
            if (backdrop) {
                backdrop.remove();
            }
            document.body.classList.remove('modal-open');

            setTimeout(() => {
                if (!document.querySelector('.modal.show')) {
                    document.body.classList.remove('modal-open');
                    let backdrops = document.querySelectorAll('.modal-backdrop');
                    backdrops.forEach(backdrop => backdrop.remove());
                }
            }, 500);
        }
    } else {
        console.error("Modal element not found");
    }
}

function openBreakModal() {
    const modalElement = new bootstrap.Modal(document.getElementById('breakModal'), {
        backdrop: 'static',
        keyboard: true
    });
    modalElement.show();
}

function closeBreakModal() {
    var modalElement = document.getElementById('breakModal');
    if (modalElement) {
        var modal = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
        modal.hide();
        modal.hide();
        modal.hide();

        const backdrop = document.querySelector('.modal-backdrop');
        if (backdrop) {
            backdrop.remove();
        }
        document.body.classList.remove('modal-open');

        setTimeout(() => {
            if (!document.querySelector('.modal.show')) {
                document.body.classList.remove('modal-open');
                let backdrops = document.querySelectorAll('.modal-backdrop');
                backdrops.forEach(backdrop => backdrop.remove());
            }
        }, 300);
    }
}

function openBreakTimerModal() {
    const modalElement = new bootstrap.Modal(document.getElementById('breakTimerModal'), {
        backdrop: 'static',
        keyboard: true
    });
    setTimeout(() => {
        modalElement.show();
    }, 700);
}

function closeBreakTimerModal() {
    var modalElement = document.getElementById('breakTimerModal');
    if (modalElement) {
        var modal = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
        modal.hide();
        modal.hide();
        modal.hide();

        const backdrop = document.querySelector('.modal-backdrop');
        if (backdrop) {
            backdrop.remove();
        }
        document.body.classList.remove('modal-open');

        setTimeout(() => {
            if (!document.querySelector('.modal.show')) {
                document.body.classList.remove('modal-open');
                let backdrops = document.querySelectorAll('.modal-backdrop');
                backdrops.forEach(backdrop => backdrop.remove());
            }
        }, 500);
    }
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
            modal.hide();
            modal.hide();

            const backdrop = document.querySelector('.modal-backdrop');
            if (backdrop) {
                backdrop.remove();
            }
            document.body.classList.remove('modal-open');

            setTimeout(() => {
                if (!document.querySelector('.modal.show')) {
                    document.body.classList.remove('modal-open');
                    let backdrops = document.querySelectorAll('.modal-backdrop');
                    backdrops.forEach(backdrop => backdrop.remove());
                }
            }, 500);
        }
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
            modal.hide();
            modal.hide();

            const backdrop = document.querySelector('.modal-backdrop');
            if (backdrop) {
                backdrop.remove();
            }
            document.body.classList.remove('modal-open');

            setTimeout(() => {
                if (!document.querySelector('.modal.show')) {
                    document.body.classList.remove('modal-open');
                    let backdrops = document.querySelectorAll('.modal-backdrop');
                    backdrops.forEach(backdrop => backdrop.remove());
                }
            }, 500);
        }
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
            modal.hide();
            modal.hide();

            const backdrop = document.querySelector('.modal-backdrop');
            if (backdrop) {
                backdrop.remove();
            }
            document.body.classList.remove('modal-open');

            setTimeout(() => {
                if (!document.querySelector('.modal.show')) {
                    document.body.classList.remove('modal-open');
                    let backdrops = document.querySelectorAll('.modal-backdrop');
                    backdrops.forEach(backdrop => backdrop.remove());
                }
            }, 500);
        }
    } else {
        console.error("Modal element not found");
    }
}

//login networkmodal
function openLoginNetworkModal() {
    const modalElement = new bootstrap.Modal(document.getElementById('loginNetworkModal'), {
        backdrop: 'static',
        keyboard: true
    });
    modalElement.show();
}
function closeLoginNetworkModal() {
    var modalElement = document.getElementById('loginNetworkModal');
    if (modalElement) {
        var modal = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
        if (modal) {
            modal.hide();
            modal.hide();
            modal.hide();

            const backdrop = document.querySelector('.modal-backdrop');
            if (backdrop) {
                backdrop.remove();
            }
            document.body.classList.remove('modal-open');

            setTimeout(() => {
                if (!document.querySelector('.modal.show')) {
                    document.body.classList.remove('modal-open');
                    let backdrops = document.querySelectorAll('.modal-backdrop');
                    backdrops.forEach(backdrop => backdrop.remove());
                }
            }, 500);
        }
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
            modal.hide();
            modal.hide();

            const backdrop = document.querySelector('.modal-backdrop');
            if (backdrop) {
                backdrop.remove();
            }
            document.body.classList.remove('modal-open');

            setTimeout(() => {
                if (!document.querySelector('.modal.show')) {
                    document.body.classList.remove('modal-open');
                    let backdrops = document.querySelectorAll('.modal-backdrop');
                    backdrops.forEach(backdrop => backdrop.remove());
                }
            }, 500);
        }
    } else {
        console.error("Modal element not found");
    }
}

function openUpdateModal() {
    const modalElement = new bootstrap.Modal(document.getElementById('updateModal'), {
        backdrop: 'static',
        keyboard: true
    });
    modalElement.show();
}
function closeUpdateModal() {
    var modalElement = document.getElementById('updateModal');
    if (modalElement) {
        var modal = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
        if (modal) {
            modal.hide();
            modal.hide();
            modal.hide();

            const backdrop = document.querySelector('.modal-backdrop');
            if (backdrop) {
                backdrop.remove();
            }
            document.body.classList.remove('modal-open');

            setTimeout(() => {
                if (!document.querySelector('.modal.show')) {
                    document.body.classList.remove('modal-open');
                    let backdrops = document.querySelectorAll('.modal-backdrop');
                    backdrops.forEach(backdrop => backdrop.remove());
                }
            }, 500);
        }
    } else {
        console.error("Modal element not found");
    }
}

function openUpdateInprogressModal() {
    const modalElement = new bootstrap.Modal(document.getElementById('updateInprogressModal'), {
        backdrop: 'static',
        keyboard: true
    });
    modalElement.show();
}
function closeUpdateInprogressModal() {
    var modalElement = document.getElementById('updateInprogressModal');
    if (modalElement) {
        var modal = bootstrap.Modal.getInstance(modalElement) || new bootstrap.Modal(modalElement);
        if (modal) {
            modal.hide();
            modal.hide();
            modal.hide();

            const backdrop = document.querySelector('.modal-backdrop');
            if (backdrop) {
                backdrop.remove();
            }
            document.body.classList.remove('modal-open');

            setTimeout(() => {
                if (!document.querySelector('.modal.show')) {
                    document.body.classList.remove('modal-open');
                    let backdrops = document.querySelectorAll('.modal-backdrop');
                    backdrops.forEach(backdrop => backdrop.remove());
                }
            }, 500);
        }
    } else {
        console.error("Modal element not found");
    }
}
function Download() {
    const url = 'https://hublog.org/downloads';
    const a = document.createElement('a');
    a.href = url;
    //a.download = 'Hublog.exe';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
}

//audio handling
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
        audioElement.pause();
        audioElement.currentTime = 0; // Reset the audio to the start
    }
};

function PunchInAudio() {
    var audioElement = document.getElementById("punchInPlayer");
    if (audioElement) {
        audioElement.play();
    }
};

function PunchOutAudio() {
    var audioElement = document.getElementById("punchOutPlayer");
    if (audioElement) {
        audioElement.play();
    }
};

function updateProgressBar(percent) {
    const progressBar = document.getElementById('progressBar');
    progressBar.style.width = percent + '%'; // Update the width
    progressBar.textContent = percent + '%'; // Update the text
}

function simulateProgress() {
    let progress = 0;

    const interval = setInterval(() => {
        if (progress >= 100) {
            clearInterval(interval); // Stop when progress reaches 100%
            alert('Download Complete!');
        } else {
            progress += 10; // Increment progress
            updateProgressBar(progress);
        }
    }, 500); // Update every 500ms
}
