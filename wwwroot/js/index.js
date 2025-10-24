$(document).ready(function () {
    const btnUplod = document.getElementById("btnUpload");
    const fileInput = document.getElementById('FileUpload_FormFile');
    const downloadForm = document.getElementById('downloadForm');
    fileInput.addEventListener("change", async (event) => {

        const file = event.target.files[0];
        if (event.target.files[0].size > 50 * 1024 * 1024) {
            alert("File size exceeds 50 MB limit.");
            this.value = ""; // reset
        }
        $('#lblFileName').text(file.name);

    });
    FillVoices();



    btnUplod.addEventListener("click", async (event) => {
        var _url = $("#videoUrl").val();
        if (_url.length > 0) {
            startDownload();
        }
        else {
            var progress = $('<div class="progress mb-2"><div class="progress-bar progress-bar-striped progress-bar-animated" role="progressbar" aria-valuenow="75" aria-valuemin="0" aria-valuemax="100" style="width:0%" ></div></div>');
            $('#upprogress').prepend(progress);
            const file = fileInput.files[0];
            if (file) {
                try {
                    $('.progress-bar').clone(true);
                    await uploadFileChunks(file, progress);

                } catch (error) {
                    console.error("File upload failed:", error);
                }
            }
        }



    });

    var videoElement = document.getElementById('myVideo');
    videoElement.on
    playing = function () {
        var track = $(this);
        track[0].textTracks[0].addEventListener("cuechange", (e) => {
            var txt = e.currentTarget.activeCues[0].text;
            $('#subtitles').text(txt);
        });
    };
    // Add submit event listener to the form
    if (downloadForm) {
        downloadForm.addEventListener('submit', function (e) {
            e.preventDefault();
            downloadSelectedFiles();
        });
    }
});
let connection = new signalR.HubConnectionBuilder()
    .withUrl("/downloadHub")
    .build();

connection.on("ProgressUpdate", percent => {
    var progress = $('<div class="progress mb-2"><div class="progress-bar progress-bar-striped progress-bar-animated" role="progressbar" aria-valuenow="75" aria-valuemin="0" aria-valuemax="100" style="width:0%" ></div></div>');
    $('#upprogress').prepend(progress);
    $(progress).find('.progress-bar').css("width", percent + "%");
    
});

connection.on("DownloadComplete", file => {
   /* document.getElementById("status").innerText = "✅ Download Complete: " + file;*/
    $("#uploadedfile").val(file);
    $('#myVideo source').attr('src', file);
    // $('#myVideo track').attr('src', data[data.length - 1].vttPath);
    var $video = $('#myVideo');
    $video[0].load();
});

connection.start().then(() => {
    console.log("Connected to SignalR");
});

function TranslateVidio() {
    var fileInput = $("#uploadedfile").val();
    var loader = document.querySelector('.loader');
    var btnText = btnsubmit.querySelector('.btn-text');   
    var sourcelanguage = $("#transLang option:selected").val();
    var targetlanguage = $("#targetLanguage option:selected").val();
    var voiceid = $("#ddlVoics option:selected").val();
    var targetlanguages = [targetlanguage];
    
    var formData = new FormData();  
   
    // Else, check for file upload
   if (fileInput!="") {
       formData.append("uploaded_mp4file", fileInput); // Actual file object
    }
    else {
        alert("Please select a file or enter a video URL.");
        return;
    }

    formData.append("source_language", sourcelanguage);
    
    formData.append("targetlanguage", targetlanguage); 
    formData.append("voiceid", voiceid);
    formData.append("output_format", "vtt");
    btnText.style.display = 'none';      // hide text
    btnsubmit.disabled = true;
    loader.style.display = 'inline-block';
   
    $.ajax({
        url: "/api/TranslateVideo/Translate",
        type: "POST",
        data: formData,
        contentType: false,        // Prevent jQuery from setting content type
        processData: false,        // Prevent jQuery from processing the data
        success: function (data) {
            setTimeout(2000);
            var videoUrl = data;
            var translatedVideoSource = document.getElementById('translatedVideoSource');
            const newUrl = `${videoUrl}?t=${Date.now()}`;
            
            translatedVideoSource.src = newUrl;
            btnsubmit.disabled = false;
            loader.style.display = 'none';
            btnText.style.display = 'inline';
            document.getElementById('translatedVideoPlayer').load();
        },
        error: function (e) {
            btnsubmit.disabled = false;
            loader.style.display = 'none';
            btnText.style.display = 'inline';
            alert("Transcription failed.");
        }
    });
}
function RegenerateVideo() {
    var fileInput = $("#uploadedfile").val();
    const translatedVideoSource = document.getElementById('translatedVideoSource');
    const translatedvideoplayer = document.getElementById('translatedVideoPlayer');
    var loader = document.querySelector('#btnregenerate .loader');
    const resubmit = document.getElementById('btnregenerate');
    var btnText = resubmit.querySelector('.btn-text2');
    var sourcelanguage = $("#transLang option:selected").val();
    var targetlanguage = $("#targetLanguage option:selected").val();
    var voiceid = $("#ddlVoics option:selected").val();
    var targetlanguages = [targetlanguage];

    var formData = new FormData();

    // Else, check for file upload
    if (fileInput != "") {
        formData.append("uploaded_mp4file", fileInput); // Actual file object
    }
    else {
        alert("Please select a file or enter a video URL.");
        return;
    }
    formData.append("voiceid", voiceid);
    formData.append("source_language", sourcelanguage);
    
    formData.append("targetlanguage", targetlanguage);

    btnText.style.display = 'none';      // hide text
    btnsubmit.disabled = true;
    loader.style.display = 'inline-block';
    // Send AJAX POST request with FormData
    $.ajax({
        url: "/api/TranslateVideo/ReTranslate",
        type: "POST",
        data: formData,
        contentType: false,        // Prevent jQuery from setting content type
        processData: false,        // Prevent jQuery from processing the data
        success: function (data) {
            setTimeout(2000);
            var videoUrl = data;
            const newUrl = `${videoUrl}?t=${Date.now()}`;    
            translatedVideoSource.src = newUrl;
            btnsubmit.disabled = false;
            loader.style.display = 'none';
            btnText.style.display = 'inline';
            translatedvideoplayer.load();
        },
        error: function (e) {
            btnsubmit.disabled = false;
            loader.style.display = 'none';
            btnText.style.display = 'inline';
            alert("Transcription failed.");
        }
    });

}


async function startDownload() {
    const url = document.getElementById("videoUrl").value;
    const connectionId = connection.connectionId; // unique for client

    await fetch(`/api/Download/DownloadVideo?url=${encodeURIComponent(url)}&connectionId=${connectionId}`, {
        method: "POST"
    });
}
const chunkSize = 1024 * 1024;

async function uploadFileChunks(file, progress) {

    const fileSize = file.size;
    const totalChunks = Math.ceil(fileSize / chunkSize);

    // Iterate over the chunks and upload them
    for (let chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++) {
        // Calculate the start and end byte positions for the current chunk
        const start = chunkIndex * chunkSize;
        const end = Math.min(start + chunkSize, fileSize);
        const isLastChunk = chunkIndex === totalChunks - 1;
        // Create a FormData object to hold the chunk data
        const formData = new FormData();
        formData.append("chunk", file.slice(start, end), file.name);
        formData.append("chunkIndex", chunkIndex);
        formData.append("totalChunks", totalChunks);
        formData.append("isLastChunk", isLastChunk);
        const percentComplete = (chunkIndex / totalChunks) * 100;
        // Set the appropriate Content-Range header for the current chunk
        const headers = { "Content-Range": `bytes ${start}-${end - 1}/${fileSize}` };

        // Make the HTTP request and upload the chunk
        const response = await fetch("/api/Upload/UploadChunk", {
            method: "POST",
            body: formData,
            headers: headers
        });
        // Process the response as needed
        // ...
        // Ensure the response was successful before proceeding to the next chunk
        if (!response.ok) {
            throw new Error(`Chunk upload failed for chunk ${chunkIndex}`);
        }
        else
            $(progress).find('.progress-bar').css("width", percentComplete + "%");
        if (isLastChunk) {
           
            $("#videoContainer").show();
            const data = await response.json();
            var Imgicon = data.iconUri;
            var vidioUrl = data.vidioUrl;
            $("#uploadedfile").val(vidioUrl);
            $('#myVideo source').attr('src', vidioUrl);
            // $('#myVideo track').attr('src', data[data.length - 1].vttPath);
            var $video = $('#myVideo');
            $video[0].load();
        }
    }
    progress.remove();
    // $(progress+" .progress-bar").css("width", "100%");

}
function FillVoices() {

    $.ajax({
        url: "/api/TranslateVideo/GetVoices",
        type: "GET",
        traditional: true,       
        success: function (data) {
            var dropdownMenu = $('#ddlVoics');
            var options = '';
            for (var i = 0; i < data.voices.length; i++) {
                options += `<option value=${data.voices[i].voice_id}>${data.voices[i].name}(${data.voices[i].labels.gender})</option>`;
                dropdownMenu.append(options);
            }
           
           

        },
        error: function (e) {

            alert("Transcription failed.");
        }
    });

}

// Function to download selected files
function downloadSelectedFiles() {
   

    const checkboxes = document.querySelectorAll('#downloadForm input[type="checkbox"]:checked');

    if (checkboxes.length === 0) {
        alert('Please select at least one file to download');
        return;
    }

     var translatedVideoSrc = document.getElementById('translatedVideoSource').getAttribute('src');
    var originalVideoSrc = document.getElementById('myVideo').querySelector('source').getAttribute('src');
    translatedVideoSrc = translatedVideoSrc.split('?')[0];
    originalVideoSrc = originalVideoSrc.split('?')[0];

    // Get current selected language for naming files
    const targetLanguage = document.getElementById('targetLanguage').value;

    // Process each selected file
    checkboxes.forEach(function (checkbox) {
        const fileType = checkbox.name;
        let fileUrl = '';
       // string langcode = _languageCoderHelper.GetLanguageCode(targetLanguage);

        // Determine the correct file URL based on checkbox name
        switch (fileType) {
            case 'translated_video':
                fileUrl = translatedVideoSrc;
                break;
            case 'translated_audio':
                // Extract audio URL from video path by changing extension
                fileUrl = translatedVideoSrc.replace('.mp4', '.mp3');
                break;
            case 'source_transcript':
                // Assume VTT file has same name as original video but with .vtt extension
                fileUrl = originalVideoSrc.replace('.mp4', '.vtt');
                break;
            case 'language_transcript':
                // Assume translated VTT has language code in filename
                fileUrl = translatedVideoSrc.replace('.mp4', '.vtt');
                break;
            default:
                console.error('Unknown file type:', fileType);
                return;
        }

        if (fileUrl && fileUrl !== '') {
     
  
            triggerDownload(fileUrl, fileUrl);
        }
    });
}

// Helper function to trigger a file download
function triggerDownload(url, fileName) {
    
    if (!url.startsWith('http') && !url.startsWith('/')) {
        url = '/' + url;
    }
    let cleanfilename = fileName.split('/').pop();
    if (cleanfilename.startsWith('_Uploads_')) {
        cleanfilename = cleanfilename.replace('_Uploads_', '');
    }
    const a = document.createElement('a');
    a.style.display = 'none';
    a.href = url;
    a.download = cleanfilename;
    document.body.appendChild(a);
    a.click();

    // Clean up
    setTimeout(() => {
        document.body.removeChild(a);
    }, 100);
}
