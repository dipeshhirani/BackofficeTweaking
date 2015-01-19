﻿(function () {

    var count = 1,
        event = function (event) {
            $(".nav-tabs > li").show();  // Display any tab in order to fixe a bug regarding dashboard tabs
            $(".umb-tab-buttons > div > a").show();
            if (event.animationName == 'nodeInserted') {
                var scope = angular.element(event.target).scope();

                if (scope && $(event.target).hasClass('umb-property')) {

                    // Tabs
                    if (scope.property && scope.property.config && scope.property.config.hidetabs) {
                        console.log(scope);
                        var tabLabels = scope.property.config.hidetabs.split(",");

                        for (var i = 0; i < tabLabels.length; i++) {
                            $(".nav-tabs > li > a:contains('" + tabLabels[i] + "')").addClass('hidden-tab');
                        }
                    }

                    // Properties
                    if (scope.property && scope.property.config && scope.property.config.hide) {
                        $(event.target).hide();
                    }

                    // Buttons
                    if (scope.property && scope.property.config && scope.property.config.hidebuttons) {
                        var buttons = scope.property.config.hidebuttons.split(",");
                        for (var i = 0; i < buttons.length; i++) {

                        }
                    }

                    $(".controls", $(event.target)).addClass('show-controls');
                    $(".nav-tabs").addClass("show-tabs");
                }
            }
        }

    document.addEventListener('animationstart', event, false);
    document.addEventListener('MSAnimationStart', event, false);
    document.addEventListener('webkitAnimationStart', event, false);
})();
