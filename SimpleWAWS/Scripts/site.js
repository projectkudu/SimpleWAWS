var wawsSiteCookie = "WAWSSiteId";
var idCookieValue = "Id";

var Template = (function () {
    function Template(json) {
        this.id = json.id;
        this.name = json.name;
        this.fileName = json.fileName;
        this.repo = json.repo;
        this.language = json.language;
    }
    return Template;
})();

var viewModel;

function initViewModel() {
    viewModel = this;
    viewModel.selectedLanguage = ko.observable();
    viewModel.selectedTemplate = ko.observable();
    viewModel.templates = ko.observableArray();
    viewModel.languages = ko.computed(function () {
        var languages = ko.utils.arrayMap(viewModel.templates(), function (item) {
            return item.language;
        });
        return ko.utils.arrayGetDistinctValues(languages).sort();
    });
    viewModel.selectLanguage = function (event) {
        $(".languages .btn").removeClass("active");
        $(event.target).addClass("active");
        viewModel.selectedLanguage($(event.target).text());
        $(".templates .btn").first().click();
    };
    viewModel.selectTemplate = function(event) {
        $(".templates .btn").removeClass("active");
        $(event.target).addClass("active");
    };

    ko.applyBindings(viewModel);
}

function initTemplates() {
    $.getJSON("/api/templates", function (data) {
        for (var i = 0; i < data.length; i++) {
            viewModel.templates.push(new Template(data[i]));
        }
    }).done(function () {
        $(".languages .btn").first().click();
    });
}

function initSite() {
    var cookie = $.cookie(wawsSiteCookie);
    if (cookie !== undefined) {
        $.getJSON("", function (data) {
        });
    }
}

window.onload = function () {
    initViewModel();
    initTemplates();
    initSite();
};