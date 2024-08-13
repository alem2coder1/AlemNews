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

    newsTitles.forEach(function (title) {
        if (title.textContent.length > 40) {
            title.textContent = title.textContent.substring(0, 40) + '...';
        }
    });
    $(function () {
        // Initializing when the document is ready
        $('button.btn-play-speech').each(function () {
            let $btn = $(this);
            let containerDiv = $btn.closest('div.article-view');
            let combinedText = '';
            containerDiv.find('.read-text').each(function () {
                combinedText += $(this).text() + ' ';
            });
            combinedText = combinedText.trim();
            $btn.attr('data-text', combinedText);
        });

        // Handling click event for all buttons
        $('button.btn-play-speech').click(function () {
            let $btn = $(this),
                $icon = $btn.find('i'),
                requestData = {
                    text: $btn.attr('data-text')
                };

            $btn.prop("disabled", true);
            $icon.addClass('fa-fade');

            $.ajax({
                type: "POST",
                url: "/" + $qar.getCurrentLanguage() + "/Home/TextToSpeech",
                contentType: "application/json",
                data: JSON.stringify(requestData),
                success: function (response, status, xhr) {
                    var blob = new Blob([response], { type: "audio/wav" });
                    var url = window.URL.createObjectURL(blob);
                    var audio = new Audio();
                    audio.src = url;
                    audio.play()
                        .then(() => {
                        })
                        .catch(e => {
                            console.error("Error playing audio: ", e);
                            $btn.prop("disabled", false);
                            $icon.removeClass('fa-fade');
                        });
                    audio.onended = function () {
                        $btn.prop("disabled", false);
                        $icon.removeClass('fa-fade');
                    };
                },
                error: function (xhr, status, error) {
                    console.error("Error: " + error);
                    $btn.prop("disabled", false);
                    $icon.removeClass('fa-fade');
                },
                xhrFields: {
                    responseType: 'blob'
                }
            });
        });
    });

});
