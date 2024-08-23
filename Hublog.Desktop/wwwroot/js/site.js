
function openBreakModal() {
    console.log("openBreakModal called");
    const modalElement = new bootstrap.Modal(document.getElementById('breakModal'));
    modalElement.show();
}


function closeBreakModal() {
    const modalElement = bootstrap.Modal.getInstance(document.getElementById('breakModal'));
    modalElement.hide();
}

// Draggable functionality
const modalDialog = document.querySelector('.modal-dialog');
const modalHeader = document.querySelector('.modal-header');

let isDragging = false;
let offsetX, offsetY;

modalHeader.addEventListener('mousedown', (e) => {
    isDragging = true;
    offsetX = e.clientX - modalDialog.getBoundingClientRect().left;
    offsetY = e.clientY - modalDialog.getBoundingClientRect().top;
    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
});

function onMouseMove(e) {
    if (isDragging) {
        modalDialog.style.position = 'absolute';
        modalDialog.style.left = `${e.clientX - offsetX}px`;
        modalDialog.style.top = `${e.clientY - offsetY}px`;
    }
}

function onMouseUp() {
    isDragging = false;
    document.removeEventListener('mousemove', onMouseMove);
    document.removeEventListener('mouseup', onMouseUp);
}

