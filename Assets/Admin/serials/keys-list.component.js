angular.module('serialsApp').component('keysList', {
    templateUrl: 'keys-list.template.html',
    controller: function SerialsController($http) {
        var self = this;
        var account = window.appInfos.accountId;
        var application = window.appInfos.application;
        var xToken = encodeURIComponent(window.appInfos.token);
        this.skip = 0;
        this.take = 20;
        this.query = '';
        this.comment = '';
        this.keyType = 'default';
        this.createKeysCount = 1;

        this.serials = [

        ];

        this.next = function () {
            self.skip += 20;
            self.refreshSerials();
        }
        this.previous = function () {
            if (self.skip >= 20) {
                self.skip -= 20;
            }
            self.refreshSerials();
        }
        this.refreshSerials = function () {
            $http.get('/' + account + '/' + application + '/_admin/_serials/search?query=' + self.query + '&skip=' + self.skip + '&take=' + self.take + '&x-token=' + xToken).then(function (response) {
                self.serials = response.data;

            });
        };

        this.generateKeys = function () {
            $http.post('/' + account + '/' + application + '/_admin/_serials/CreateKeys?x-token=' + xToken,
			{
			    'count': self.createKeysCount,
			    'content': JSON.stringify({ 'type': self.keyType }),
			    'comment': self.comment

			})
			.then(function (response) {

			    var text = '';
			    response.data.forEach(function (key) {
			        text += key + '\n';
			    });
			    var a = document.createElement("a");
			    var file = new Blob([text], { type: "text/csv" });
			    a.href = URL.createObjectURL(file);
			    a.download = "serials.csv";
			    a.click();

			});
        };
        this.refreshSerials();
    }
});
