/**
 * Alerts Controller
 */

angular
    .module('RDash')
    .controller('AlertsCtrl', ['$scope', AlertsCtrl]);

function AlertsCtrl($scope) {
    $scope.alerts = [];

    $scope.addAlert = function() {
        // $scope.alerts.push({
        //     msg: 'Another alert!'
        // });
    };

    $scope.closeAlert = function(index) {
        $scope.alerts.splice(index, 1);
    };
}