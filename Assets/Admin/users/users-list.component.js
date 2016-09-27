angular.module('usersApp').component('usersList', {
    templateUrl: 'users-list.template.html',
    controller: function SerialsController($http) {
        var self = this;
        var account = window.appInfos.accountId;
        var application = window.appInfos.application;
        var xToken = encodeURIComponent(window.appInfos.token);
        this.skip = 0;
        this.take = 20;
        this.query = '';
        this.exportTake = 1000;
        this.exportSkip = 0;


        this.users = [

        ];

        this.next = function () {
            self.skip += 20;
            self.refreshUsers();
        };
        this.previous = function () {
            if (self.skip >= 20) {
                self.skip -= 20;
            }
            self.refreshUsers();
        };
        this.refreshUsers = function () {
            $http.get('/' + account + '/' + application + '/_admin/_users/search?query=' + self.query + '&skip=' + self.skip + '&take=' + self.take + '&x-token=' + xToken).then(function (response) {
                self.users = response.data;

            });
        };
        this.export = function () {
            $http.get('/' + account + '/' + application + '/_admin/_users/search?query=&skip=' + self.exportSkip + '&take=' + self.exportTake + '&x-token=' + xToken)
            .then(function (response) {

                var text = '';
                response.data.forEach(function (usr) {
                    text += usr.channels.email.value + ',' + usr.userData.pseudo + ',' + usr.userData.key + '\n';
                });
                var a = document.createElement("a");
                var file = new Blob([text], { type: "text/csv" });
                a.href = URL.createObjectURL(file);
                a.download = "users.csv";
                a.click();

            });
        };
        this.delete = function (user) {
            $http.delete('/' + account + '/' + application + '/_admin/_users/' + user.id + '?x-token=' + xToken)
            .then(function (response) {
                self.refreshUsers();
            });
        };
        this.refreshUsers();
    }
});
