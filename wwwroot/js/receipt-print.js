function printReceipt() {
    var receiptCard = document.getElementById('receiptCard').outerHTML;
    var printWindow = window.open('', '', 'height=600,width=800');
    printWindow.document.write('<!DOCTYPE html><html><head><title>বিক্রয় রসিদ</title><meta charset="UTF-8"><style>body{margin:20px;font-family:Arial,sans-serif}.card{background:white;border:none;padding:20px}.text-center{text-align:center}.d-flex{display:flex;justify-content:space-between}hr{border:1px solid #999;margin:10px 0}h4,h5{margin:5px 0}.small{font-size:12px}.mb-0{margin-bottom:0}.mb-1{margin-bottom:5px}.mb-2{margin-bottom:10px}.mb-3{margin-bottom:15px}.mb-4{margin-bottom:20px}.p-4{padding:20px}</style></head><body>');
    printWindow.document.write(receiptCard);
    printWindow.document.write('</body></html>');
    printWindow.document.close();
    setTimeout(function() {
        printWindow.print();
    }, 250);
}
