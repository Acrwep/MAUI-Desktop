//async function captureScreenshot() {
//    return new Promise((resolve, reject) => {
//        html2canvas(document.body).then(canvas => {
//            canvas.toBlob(blob => {
//                const reader = new FileReader();
//                reader.onloadend = () => resolve(reader.result);
//                reader.readAsDataURL(blob);
//            });
//        }).catch(error => reject(error));
//    });
//}

function captureScreenshot() {
    return new Promise((resolve, reject) => {
        html2canvas(document.body).then(canvas => {
            resolve(canvas.toDataURL());
        }).catch(error => {
            reject(error);
        });
    });
}


