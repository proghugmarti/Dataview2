let dotNetObject;
var _currentPage = 0;
var _pageSize = 0;
var _totalFiles = 0;
var _totalPages = 0;
var _maxPageIndex = 0;
function registerDotNetObject(objRef) {
    dotNetObject = objRef;
}
function UpdateSurveys(direction, incPage) {
    if (direction == "next")
        _currentPage = _currentPage + incPage;
    else if (direction == "prev")
        _currentPage = _currentPage - incPage;
    else if (direction == "first")
        _currentPage = incPage;
    else if (direction == "last")
        _currentPage = incPage;


    if (dotNetObject) {
        dotNetObject.invokeMethodAsync('UpdateProcessingObjectsChk', _currentPage)
            .then(() => {
                console.log("Mud controls setup up correctly.");
            })
            .catch(error => {
                console.error("Error setting up the Mud controls.:", error);
            });
    } else {
        console.error("DotNetObject is not registered.");
    }
}

function assignNextPageClickEvent(pageSize, totalFiles) {

    _pageSize = pageSize;
    _totalFiles = totalFiles;
    _totalPages = Math.ceil(_totalFiles / _pageSize);
    _maxPageIndex = _totalPages - 1;
    var incPage = 1;

    const nextPageButton = document.querySelector('[aria-label="Next page"]');   
    if (nextPageButton) {
        nextPageButton.onclick = function () {
            UpdateSurveys("next",  incPage);
        };
    }

    const prevPageButton = document.querySelector('[aria-label="Previous page"]');
    if (prevPageButton) {
        prevPageButton.onclick = function () {
            UpdateSurveys("prev", incPage);
        };
    }

    const firstPageButton = document.querySelector('[aria-label="First page"]');
    if (firstPageButton) {
        firstPageButton.onclick = function () {
            UpdateSurveys("first", 0);
        };
    }

    const lastPageButton = document.querySelector('[aria-label="Last page"]');
    if (lastPageButton) {
        lastPageButton.onclick = function () {
            UpdateSurveys("last", _maxPageIndex);
        };
    }
}