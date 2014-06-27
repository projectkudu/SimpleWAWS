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
            viewModel.selectLanguage(getCorrectDefaultLanguage(viewModel.templates()));
        }
    });
}

function getCorrectDefaultLanguage(templates) {
    var regex = new RegExp("[\\?&]language=([^&#]*)"),
            results = regex.exec(location.search);
    result = results === null ? null : decodeURIComponent(results[1].replace(/\+/g, " "));
    if (result === null) {
        return templates[0].language;
    } else {
        result = result.toUpperCase();
        for (var i = 0; i < templates.length; i++) {
            if (templates[i].language.toUpperCase() === result) {
                return templates[i].language;
            }
        }
        return templates[0].language
    }
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
        var expireDateTime = new Date();
        expireDateTime.setMinutes(expireDateTime.getMinutes() + parseInt(match[1]));
        expireDateTime.setSeconds(expireDateTime.getSeconds() + parseInt(match[4]));
        countDown(expireDateTime);
    }
}

function countDown(expireDateTime) {
    if (viewModel.siteJson() != undefined) {
        var now = new Date();
        var diff = expireDateTime - now;
        if (diff <= 0) {
            viewModel.timeLeft("00m:00s");
            $("#site-expired").show();
            return;
        }
        diff = diff / 1000;
        viewModel.timeLeft(("0" +Math.floor(diff/60)).slice(-2) + "m:" + ("0" + Math.floor(diff%60)).slice(-2) + "s");
        currentTimeout = setTimeout(countDown, 1000, expireDateTime);
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
        deleteSite();
        $("#site-expired").hide();
    });
};