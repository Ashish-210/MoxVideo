var grid;
$(document).ready(function () {


    grid=$('#grid').grid({
        dataSource: '/api/UploadFile/UploadInfo',
        uiLibrary: 'bootstrap5',
        iconsLibrary: 'fontawesome',
        navigatable: true,
        bodyRowHeight: 'fixed',
        columns: [
            { field: 'filename', title: 'Name', width: 200 },
            { field: 'size', title: 'Size', width: 100 },
            { field: 'format', title: 'Type', width: 100 },
            { field: 'uploaddate', title: 'UploadDate', width: 120 },
            { title: '', field: 'Delete', width: 32, type: 'icon', icon: 'fas fa-trash', events: { 'click': function (e) { DeleteFile(e) }  } }
        ],
      
        pager: { limit: 10 ,sizes:[10,20,50]}
    })

    
})
function DeleteFile(e) {
    debugger
    var filename = e.data.record.filename;
    
        $.ajax({
            url: '/api/UploadFile/DeleteFile?fileName=' + encodeURIComponent(filename),
        type: 'DELETE',
       
            success: function (result) {
             //   alert(result);
                grid.reload();
        }, error(error) {
            console.log(error);
        }
    })
}