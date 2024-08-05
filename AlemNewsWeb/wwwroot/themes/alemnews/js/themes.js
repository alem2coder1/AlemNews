document.addEventListener("DOMContentLoaded", function () {
    document.body.addEventListener("submit", (e) => {
        const that = e.target;

        if (that.classList.contains("uly-search")) {
            e.preventDefault();
            const keyword = that.querySelector("input.uly-input-search").value,
                url = that.getAttribute("action");
            window.location.href = url + `?keyword=${keyword}`;
        }
    });
    const elements = document.querySelectorAll('.ulytitle');
    if (elements) {
        elements.forEach(element => {
            if (element.textContent.length > 30) {
                element.textContent = element.textContent.substring(0, 30) + '...';
            }
        });
    }
    const ulyblok3title = document.querySelectorAll('.ulyblok3title');
    if (ulyblok3title) {
        ulyblok3title.forEach(element => {
            if (element.textContent.length > 30) {
                element.textContent = element.textContent.substring(0, 60) + '...';
            }
        });
    }
    const toptitle = document.querySelectorAll('.toptitle');
    if (toptitle) {
        toptitle.forEach(element => {
            if (element.textContent.length > 30) {
                element.textContent = element.textContent.substring(0, 40) + '...';
            }
        });
    }
    ratings = document.querySelectorAll(".rating-part *[data-rating]");
    if (ratings && ratings.length > 0) {
        ratings.forEach((rating) => {
            rating.addEventListener("click", function () {
                const that = this,
                    id = that.getAttribute("data-id"),
                    data = new FormData(),
                    type = that.getAttribute("data-rating"),
                    count = that.querySelector(".reaction-count");

                data.append("articleId", id);
                data.append("ratingType", type);

                fetch(`/${$qar.getCurrentLanguage()}/SaveArticleRating`, {
                    method: "POST",
                    body: data,
                })
                    .then((response) => response.json())
                    .then((data) => {
                        const reactionSpan = document.querySelector(
                                ".article-reaction-span"
                            ),
                            status = data["status"];

                        if (reactionSpan) {
                            reactionSpan.innerHTML = `<span ${
                                status === "success" ? "" : `class="text-danger"`
                            }">${data["message"]}</span>`;

                            setTimeout(() => {
                                reactionSpan.innerHTML = "";
                            }, 3000);
                        }

                        if (status === "success") {
                            count.innerText = data["data"];
                            const reactions = document.querySelectorAll(".reaction");
                        }
                    });
            });
        });
    }

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

});