﻿/// <reference path="/umbraco_client/Application/NamespaceManager.js" />

Umbraco.Sys.registerNamespace("Umbraco.Controls");

(function($) {

    $.fn.ModalWindowAPI = function() {
        /// <summary>jQuery plugin exposes the modal window api for the selected object</summary>
        //if there's more than item in the selector, throw exception
        if ($(this).length != 1) {
            throw "ModalWindowAPI selector requires that there be exactly one control selected";
        };
        return $(this).data("ModalWindowAPI") == null ? null : $(this).data("ModalWindowAPI");
    };

    Umbraco.Controls.ModalWindow = function() {
        /// <summary>Modal window class, when open is called, it will create a temporary html element to attach the window to</summary>
        return {
            _wId: Umbraco.Utils.generateRandom().toString().replace(".", ""), //the modal window ID that will be assigned
            _obj: null, //the jquery element for the modal window
            _rVal: null, //a return value specified when closing that gets passed to the onCloseCallback method
            open: function(url, name, showHeader, width, height, top, leftOffset, closeTriggers, onCloseCallback) {
                /// <summary>Opens a modal window</summary>
                /// <param name="top">Optional</param>
                /// <param name="leftOffset">Optional</param>
                /// <param name="closeTriggers">
                /// Optional: An array of jQuery selectors that will trigger the modal window to close
                /// </param>
                /// <param name="onCloseCallback">
                /// A method that is called when the window is closing. the callback method will receive an instance
                /// of the jQuery object for the popup window/iframe so that you can query against the contents of the window
                /// to extract any information.
                /// The callback will receive one parameter with 2 properties:
                /// modalContent = the jQuery object for the popup window to query against
                /// outVal = the value passed to the close window method that was used to close the window (if it was specified)
                /// <returns>The generated jquery object bound to the modal window</returns>

                //check if the modal elems exist
                if (!this._modalElemsExist()) {
                    this._createModalElems();
                }

                var _this = this;

                this._obj.jqm({

                    onShow: function(h) {
                        var umbModal = $(h.w);
                        var umbModalContent = $("iframe", umbModal);

                        umbModalContent.width(width);
                        umbModalContent.height(showHeader ? height - 30 : height);

                        //remove the header if it shouldn't be shown
                        if (!showHeader) {
                            _this._obj.find(".umbModalBoxHeader").remove();
                            _this._obj.find(".umbracModalBoxClose").remove();
                        }
                        else {
                            //set the title
                            _this._obj.find(".umbModalBoxHeader").html(name);
                        }

                        //if the height is set, then set it
                        if (height > 0) {
                            umbModal.height(height);
                        }
                        //if the width is set, then set it in the center
                        if (width > 0) {
                            umbModal.width(width);
                            umbModal.css("left", (($(document).width() - width) / 2) + "px");
                        }
                        //if the top is set
                        if (top > 0) {
                            umbModal.css("top", top + "px");
                        }
                        //if the leftOffset is set
                        if (leftOffset > 0) {
                            var newLeft = parseInt(umbModal.css("left").replace("px", "")) + leftOffset;
                            umbModal.css("left", newLeft);
                        }

                        umbModalContent.html('').attr('src', _this._getUniqueUrl(url));

                        umbModal.show();
                        umbModalContent.show();

                        $(document).keyup(function(event) {
                            if (event.keyCode == 27 && umbModal.css("display") == "block") {
                                _this.close();
                            }
                        });
                        if (closeTriggers) {
                            for (var x in closeTriggers) {
                                _this._obj.jqmAddClose(closeTriggers[x]);
                            }
                        }

                    },
                    onHide: function(h) {
                        var umbModal = $(h.w);
                        var umbModalContent = $("iframe", umbModal);
                        if (typeof onCloseCallback == "function") {
                            //call the callback if specified, pass the jquery content object as a param and the output value array
                            var e = { modalContent: umbModalContent, outVal: _this._rVal };
                            onCloseCallback.call(_this, e);
                        }
                        h.w.hide();
                        h.o.remove();
                        umbModalContent.hide();
                        umbModalContent.html('').attr('src', '');
                        _this.close();
                    }
                });

                this._obj.jqmShow();
                //store the api in this objects data store
                this._obj.data("ModalWindowAPI", this);
                return this._obj;
            },
            close: function(rVal) {
                /// <summary>Closes the modal window, Removes the object from the DOM</summary>
                /// <param name="rVal">if specified, will add this parameter to the onCloseCallback method's outVal parameter so it may be used in the closing callback method
                this._rVal = rVal;
                top.focus();
                this._obj.jqmHide();
                this._obj.remove();
                return false;
            },
            _createModalElems: function() {
                /// <summary>This will create the html elements required for the modal overlay if they do not already exist in the DOM</summary>

                var overlayHtml = "<div id=\"" + this._wId + "_modal\" class=\"umbModalBox\">" +
		            "<div class=\"umbModalBoxHeader\"></div><a href=\"#\" class=\"umbracModalBoxClose jqmClose\">&times;</a>" +
		            "<div class=\"umbModalBoxContent\"><iframe frameborder=\"0\" class=\"umbModalBoxIframe\" src=\"\"></iframe></div>" +
	                "</div>";

                this._obj = $(overlayHtml).appendTo("body");

                var _this = this;
                if ($.fn.draggable) this._obj.draggable({
                    cursor: 'move',
                    distance: 5,
                    iframeFix: true,
                    helper: function(event) {
                        var o = $(this).clone();
                        o.children().remove();
                        o.css("border-width", "1px");
                        return o;
                    },
                    start: function(event, ui) {
                        ui.helper.css("z-index", 20000);
                    },
                    stop: function(event, ui) {
                        _this._obj.css("top", ui.absolutePosition.top);
                        _this._obj.css("left", ui.absolutePosition.left);
                    }
                });
            },
            _modalElemsExist: function() {
                return ($("#" + this._wId + "_modal").length > 0);
            },
            _getUniqueUrl: function(url) {
                var r = Umbraco.Utils.generateRandom();
                if (url.indexOf("?") > -1)
                    return url += "&rndo=" + r;
                else
                    return url += "?rndo=" + r;
            }
        };
    };

})(jQuery);


//
// jqModal - Minimalist Modaling with jQuery
//   (http://dev.iceburg.net/jquery/jqModal/)
//
// Copyright (c) 2007,2008 Brice Burgess <bhb@iceburg.net>
// Dual licensed under the MIT and GPL licenses:
//   http://www.opensource.org/licenses/mit-license.php
//   http://www.gnu.org/licenses/gpl.html
// 
// $Version: 07/06/2008 +r13
//
(function($) {
    $.fn.jqm = function(o) {
        var p = {
            overlay: 50,
            overlayClass: 'jqmOverlay',
            closeClass: 'jqmClose',
            trigger: '.jqModal',
            ajax: F,
            ajaxText: '',
            target: F,
            modal: F,
            toTop: F,
            onShow: F,
            onHide: F,
            onLoad: F
        };
        return this.each(function() {
            if (this._jqm) return H[this._jqm].c = $.extend({}, H[this._jqm].c, o); s++; this._jqm = s;
            H[s] = { c: $.extend(p, $.jqm.params, o), a: F, w: $(this).addClass('jqmID' + s), s: s };
            if (p.trigger) $(this).jqmAddTrigger(p.trigger);
        });
    };

    $.fn.jqmAddClose = function(e) { return hs(this, e, 'jqmHide'); };
    $.fn.jqmAddTrigger = function(e) { return hs(this, e, 'jqmShow'); };
    $.fn.jqmShow = function(t) { return this.each(function() { $.jqm.open(this._jqm, t); }); };
    $.fn.jqmHide = function(t) { return this.each(function() { $.jqm.close(this._jqm, t) }); };

    $.jqm = {
        hash: {},
        open: function(s, t) {
            var h = H[s], c = h.c, cc = '.' + c.closeClass, z = (parseInt(h.w.css('z-index'))), z = (z > 0) ? z : 3000, o = $('<div></div>').css({ height: '100%', width: '100%', position: 'fixed', left: 0, top: 0, 'z-index': z - 1 }); if (h.a) return F; h.t = t; h.a = true; h.w.css('z-index', z);
            if (c.modal) { if (!A[0]) L('bind'); A.push(s); }
            else if (c.overlay > 0) h.w.jqmAddClose(o);
            else o = F;

            h.o = (o) ? o.addClass(c.overlayClass).prependTo('body') : F;
            if (ie6) { $('html,body').css({ height: '100%', width: '100%' }); if (o) { o = o.css({ position: 'absolute' })[0]; for (var y in { Top: 1, Left: 1 }) o.style.setExpression(y.toLowerCase(), "(_=(document.documentElement.scroll" + y + " || document.body.scroll" + y + "))+'px'"); } }

            if (c.ajax) {
                var r = c.target || h.w, u = c.ajax, r = (typeof r == 'string') ? $(r, h.w) : $(r), u = (u.substr(0, 1) == '@') ? $(t).attr(u.substring(1)) : u;
                r.html(c.ajaxText).load(u, function() { if (c.onLoad) c.onLoad.call(this, h); if (cc) h.w.jqmAddClose($(cc, h.w)); e(h); });
            }
            else if (cc) h.w.jqmAddClose($(cc, h.w));

            if (c.toTop && h.o) h.w.before('<span id="jqmP' + h.w[0]._jqm + '"></span>').insertAfter(h.o);
            (c.onShow) ? c.onShow(h) : h.w.show(); e(h); return F;
        },
        close: function(s) {
            var h = H[s]; if (!h.a) return F; h.a = F;
            if (A[0]) { A.pop(); if (!A[0]) L('unbind'); }
            if (h.c.toTop && h.o) $('#jqmP' + h.w[0]._jqm).after(h.w).remove();
            if (h.c.onHide) h.c.onHide(h); else { h.w.hide(); if (h.o) h.o.remove(); } return F;
        },
        params: {}
    };
    var s = 0, H = $.jqm.hash, A = [], ie6 = $.browser.msie && ($.browser.version == "6.0"), F = false,
i = $('<iframe src="javascript:false;document.write(\'\');" class="jqm"></iframe>').css({ opacity: 0 }),
e = function(h) { if (ie6) if (h.o) h.o.html('<p style="width:100%;height:100%"/>').prepend(i); else if (!$('iframe.jqm', h.w)[0]) h.w.prepend(i); },
    //f = function(h) { try { $(':input:visible', h.w)[0].focus(); } catch (_) { } },
L = function(t) { $()[t]("keypress", m)[t]("keydown", m)[t]("mousedown", m); },
m = function(e) { var h = H[A[A.length - 1]], r = (!$(e.target).parents('.jqmID' + h.s)[0]); if (r) f(h); return !r; },
hs = function(w, t, c) {
    return w.each(function() {
        var s = this._jqm; $(t).each(function() {
            if (!this[c]) { this[c] = []; $(this).click(function() { for (var i in { jqmShow: 1, jqmHide: 1 }) for (var s in this[i]) if (H[this[i][s]]) H[this[i][s]].w[i](this); return F; }); } this[c].push(s);
        });
    });
};
})(jQuery);