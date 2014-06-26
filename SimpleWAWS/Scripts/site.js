var wawsSiteCookie = "WAWSSiteId";
var idCookieValue = "Id";

var Template = (function () {
    function Template(json) {
        this.name = json.name;
        this.fileName = json.fileName;
        this.language = json.language;
        this.icon_uri = json.icon_uri;
    }
    Template.prototype.select = function (event) {
        $(".website-template-container").removeClass("website-template-container-selected");
        var parent = $(event.target).closest("div").addClass("website-template-container-selected");
        viewModel.selectedTemplate(this);
    };
    return Template;
})();

var Site = (function () {
    function Site() {
    }
    return Site;
})();

var viewModel;

function initViewModel() {
    viewModel = this;
    viewModel.siteJson = ko.observable();
    viewModel.errorMessage = ko.observable();
    viewModel.selectedLanguage = ko.observable();
    viewModel.selectedTemplate = ko.observable();
    viewModel.templates = ko.observableArray();
    viewModel.timeLeft = ko.observable();
    viewModel.createRunning = ko.observable(false);
    viewModel.languages = ko.computed(function () {
        var languages = ko.utils.arrayMap(viewModel.templates(), function (item) {
            return item.language;
        });
        return ko.utils.arrayGetDistinctValues(languages).sort();
    });
    viewModel.selectLanguage = function (e) {
        if (typeof e === "string") {
            viewModel.selectedLanguage(e);
        } else if (e) {
            viewModel.selectedLanguage($(e.target).text());
        }
        $(".select-template-anchor").first().click();
    };
    ko.applyBindings(viewModel);
}
;

function initTemplates() {
    $.getJSON("/api/templates", function (data) {
        for (var i = 0; i < data.length; i++) {
            viewModel.templates.push(new Template(data[i]));
        }
    }).done(function () {
        if (viewModel.templates().length > 0) {
            viewModel.selectLanguage(viewModel.templates()[0].language);
        }
    });
}

function initSite() {
    toggleSpinner();
    $.ajax({
        type: "GET",
        url: "/api/site",
        data: "",
        contentType: "application/json; charset=utf-8",
        success: handleGetSite,
        error: handleGetSiteError
    });
}

var currentTimeout = 0;
function startCountDown(init) {
    if (init !== undefined) {
        var reg = '(\\d+)(m)?(:)(\\d+)(s)?';
        var pattern = new RegExp(reg, "i");
        var match = pattern.exec(init);
        countDown(parseInt(match[1]), parseInt(match[4]));
    }
}

function countDown(minutes, seconds) {
    if ( viewModel.siteJson() != undefined) {
        viewModel.timeLeft(minutes + "m:" + ("0" + seconds).slice(-2) + "s");
        seconds--;
        if (seconds === -1 && minutes !== 0) {
            seconds = 59;
            minutes--;
        } else if (seconds === -1 && minutes === 0) {
            $("#site-expired").show();
            return;
        } else if (minutes === 0 && !$(".countdown").hasClass("site-info-not-valid")) {
            $(".countdown").addClass("site-info-not-valid");
        }
        if (minutes === 0 && seconds <= 10 && !$(".countdown").hasClass("slow")) {
            $(".countdown").addClass("slow");
        }
        currentTimeout = setTimeout(countDown, 1000, minutes, seconds);
    }
}

function deleteSite(event) {
    if (event) {
        event.preventDefault();
    }
    $.ajax({
        type: "DELETE",
        url: "/api/site"
    });
    viewModel.siteJson(undefined);
    scrollToTop();
}

function toggleSpinner() {
    $("#error-message").hide();
    viewModel.createRunning(!viewModel.createRunning());
}

function handleGetSite(data) {
    toggleSpinner();
    if (data != null) {
    viewModel.siteJson(data);
    startCountDown(viewModel.siteJson().timeLeftString);
    scrollSitePartToView();
    } else {
        viewModel.siteJson(undefined);
    }
}

function scrollSitePartToView() {
    scrollHelper($("#work-with-your-site").offset().top-100);
}

function scrollToTop() {
    scrollHelper(0);
}

function scrollHelper(index) {
    $("html, body").animate({
        scrollTop: index
    }, 900);
}

function handleGetSiteError(xhr, error, errorThrown) {
    toggleSpinner();
    if (xhr.responseText) {
        var serverError = JSON.parse(xhr.responseText);
        viewModel.errorMessage(serverError.ExceptionMessage ? serverError.ExceptionMessage : serverError.Message);
    } else {
        viewModel.errorMessage("There was an error");
    }
    $("#error-message").show();
}

window.onload = function () {
    initViewModel();
    initTemplates();
    initSite();
    $("#create-site").click(function (e) {
        e.preventDefault();
        toggleSpinner();
        $.ajax({
            type: "POST",
            url: "/api/site",
            data: JSON.stringify(viewModel.selectedTemplate()),
            contentType: "application/json; charset=utf-8",
            success: handleGetSite,
            error: handleGetSiteError
        });
    });
    $("select").on("change", function (e) {
        var optionSelected = $("option:selected", this);
        var valueSelected = this.value;
        viewModel.selectLanguage(valueSelected);
    });
    $("#dismiss-site-expire").click(function (e) {
        e.preventDefault();
        $("#site-expired").hide();
        deleteSite();
    });
};