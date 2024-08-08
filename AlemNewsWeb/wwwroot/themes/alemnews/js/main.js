document.addEventListener("DOMContentLoaded", function () {

    if (document.querySelectorAll("a[data-language]")) {
        document.querySelectorAll("a[data-language]").forEach((a) => {
            a.addEventListener("click", function (event) {
                event.preventDefault();
                let aNode = this,
                    pathName = window.location.pathname,
                    currentLanguge = $qar.getCurrentLanguage(),
                    dataLanguage = aNode.getAttribute("data-language")
                        ? aNode.getAttribute("data-language").toLowerCase()
                        : "";
                if (currentLanguge === dataLanguage) return;
                if (!pathName) {
                    window.location.href = "/" + dataLanguage + window.location.search;
                }
                // if (pathName && pathName.length < currentLanguge.length) return;
                window.location.href =
                    "/" +
                    dataLanguage +
                    pathName.substring(currentLanguge.length + 1) +
                    window.location.search;
            });
        });
    }

    document.body.addEventListener("submit", (e) => {
        const that = e.target;

        if (that.classList.contains("uly-search")) {
            e.preventDefault();
            const keyword = that.querySelector("input.uly-input-search").value,
                url = that.getAttribute("action");
            window.location.href = url + `?keyword=${keyword}`;
        }
    });
    const newsTitles = document.querySelectorAll('.news-title');

    newsTitles.forEach(function(title) {
        if (title.textContent.length > 40) {
            title.textContent = title.textContent.substring(0, 40) + '...';
        }
    });

});