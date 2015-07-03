angular.module("tryApp", ["ui.router", "angular.filter"])
    .directive("selectOnClick", function() {
        //http://stackoverflow.com/a/14996261/3234163
        return {
            restrict: "A",
            link: function(scope, element, attrs) {
                element.on("click", function(s) {
                    this.select();
                }); //some stuff
            }
        };
    });