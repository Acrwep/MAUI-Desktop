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
