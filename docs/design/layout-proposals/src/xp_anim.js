// Frames of the XP animation mockups: ?f=1 (start), 2 (middle), 3 (end) set a class on <body> that each mockup styles.
(function () {
  var f = (location.search.match(/f=(\d)/) || [0, '3'])[1];
  document.body.classList.add('f' + f);
  var count = document.querySelector('[data-count]');
  if (count) count.textContent = '+' + ({ 1: 0, 2: 214, 3: 388 })[f] + ' XP';
})();
