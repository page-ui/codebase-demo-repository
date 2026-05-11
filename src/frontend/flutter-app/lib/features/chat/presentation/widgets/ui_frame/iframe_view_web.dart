import 'dart:js_interop';
import 'dart:ui_web' as ui_web;

import 'package:flutter/widgets.dart';
import 'package:web/web.dart' as web;

class IframeView extends StatelessWidget {
  const IframeView({super.key, required this.url});

  final String url;

  static final Set<String> _registeredViewTypes = <String>{};

  String get _viewType => url.isEmpty
      ? 'Page.ui-iframe-empty'
      : 'Page.ui-iframe-${Uri.encodeComponent(url)}';

  void _ensureRegistered() {
    if (_registeredViewTypes.contains(_viewType)) return;
    _registeredViewTypes.add(_viewType);
    ui_web.platformViewRegistry.registerViewFactory(_viewType, (int viewId) {
      final web.HTMLDivElement root =
          web.document.createElement('div') as web.HTMLDivElement;
      root.style
        ..width = '100%'
        ..height = '100%'
        ..overflow = 'hidden'
        ..display = 'flex'
        ..flexDirection = 'column'
        ..backgroundColor = 'transparent'
        ..borderRadius = '4px';

      if (url.trim().isEmpty) {
        final web.HTMLDivElement emptyState =
            web.document.createElement('div') as web.HTMLDivElement;
        emptyState.style
          ..flex = '1'
          ..display = 'flex'
          ..flexDirection = 'column'
          ..alignItems = 'center'
          ..justifyContent = 'center'
          ..fontFamily = 'sans-serif';

        emptyState.innerHTML =
            '''
          <div style="text-align: center; display: flex; flex-direction: column; align-items: center; justify-content: center;">
            <div style="margin-bottom: 12px; opacity: 0.4;">
              <svg width="48" height="48" viewBox="0 0 24 24" fill="#539062">
                <path d="M20 4H4c-1.1 0-2 .9-2 2v12c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm-2 14H6V6h12v12z"/>
              </svg>
            </div>
            <div style="color: white; opacity: 0.7; font-size: 16px; margin-bottom: 4px; font-weight: 500;">UI Preview</div>
            <div style="color: #E5E7EB; opacity: 0.4; font-size: 13px;">Generated UI will render here</div>
          </div>
        '''
                .toJS;
        root.appendChild(emptyState);
      } else {
        final iframe = web.HTMLIFrameElement()
          ..src = url
          ..style.border = '0'
          ..style.width = '100%'
          ..style.height = '100%'
          ..style.borderRadius = '24px'
          ..style.pointerEvents = 'auto'
          ..style.userSelect = 'auto'
          ..allow = 'clipboard-read; clipboard-write';
        root.appendChild(iframe);
      }
      return root;
    });
  }

  @override
  Widget build(BuildContext context) {
    _ensureRegistered();
    return HtmlElementView(viewType: _viewType);
  }
}
