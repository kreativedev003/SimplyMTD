window.saveAsFile = function(filename, data) {
	var blob = new Blob([data], {type: 'text/csv;charset-utf-8;'});
	saveAs(blob, filename);
}

// Show file dialog and return selected file information
window.showOpenFileDialog = async function () {
    const input = document.getElementById("file");
    input.click();

    return new Promise(resolve => {
        input.onchange = () => {
            const file = input.files[0];
            const fileInfo = {
                name: file.name,
                type: file.type,
                size: file.size,
            };
            resolve(fileInfo);
        };
    });
};

window.setFileInput = function (name) {
    const fileInput = document.querySelector('input[type="file"]');

    const myFile = new File([''], name, {
        type: 'text/plain',
        lastModified: new Date(),
    });

    // Now let's create a DataTransfer to get a FileList
    const dataTransfer = new DataTransfer();
    dataTransfer.items.add(myFile);
    fileInput.files = dataTransfer.files;
}