'use strict';

/**
 * Route configuration for the RDash module.
 */
angular.module('RDash').config(['$stateProvider', '$urlRouterProvider',
    function($stateProvider, $urlRouterProvider) {

        // For unmatched routes
        $urlRouterProvider.otherwise('/');

        // Application routes
        $stateProvider
            .state('kpis', {
                url: '/',
                templateUrl: 'templates/graph.html'
            })
            .state('appCreates', {
                url: '/appCreates',
                templateUrl: 'templates/graph.html'
            })
            .state('accountTypes', {
                url: '/accountTypes',
                templateUrl: 'templates/graph.html'
            })
            .state('referrers', {
                url: '/referrers',
                templateUrl: 'templates/graph.html'
            })
            .state('templates', {
                url: '/templates',
                templateUrl: 'templates/graph.html'
            })
            .state('experiments', {
                url: '/experiments',
                templateUrl: 'templates/experiments.html'
            })
            .state('sourceVariations', {
                url: '/sourceVariations',
                templateUrl: 'templates/sourceVariations.html'
            })
            .state('userFeedback', {
                url: '/userFeedback',
                templateUrl: 'templates/userFeedback.html'
            });
    }
]);
