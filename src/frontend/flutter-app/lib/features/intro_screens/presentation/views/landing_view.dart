import 'package:page_ui/features/intro_screens/presentation/widgets/about_section.dart';
import 'package:page_ui/features/intro_screens/presentation/widgets/features_section.dart';
import 'package:page_ui/features/intro_screens/presentation/widgets/footer_section.dart';
import 'package:page_ui/features/intro_screens/presentation/widgets/hero_section.dart';
import 'package:page_ui/features/intro_screens/presentation/widgets/landing_nav_bar.dart';
import 'package:flutter/material.dart';

class LandingView extends StatefulWidget {
  static const String routeName = "LandingView";

  const LandingView({super.key});

  @override
  State<LandingView> createState() => _LandingViewState();
}

class _LandingViewState extends State<LandingView> {
  final ScrollController _scrollController = ScrollController();

  final GlobalKey _featuresKey = GlobalKey();
  final GlobalKey _aboutKey = GlobalKey();
  final GlobalKey _footerKey = GlobalKey();

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.transparent,
      body: Stack(
        children: [
          SingleChildScrollView(
            controller: _scrollController,
            child: Column(
              children: [
                const HeroSection(),
                FeaturesSection(key: _featuresKey),
                AboutSection(key: _aboutKey),
                FooterSection(key: _footerKey),
              ],
            ),
          ),

          Positioned(
            top: 0,
            left: 0,
            right: 0,
            child: LandingNavBar(
              scrollController: _scrollController,
              featuresKey: _featuresKey,
              aboutKey: _aboutKey,
              footerKey: _footerKey,
            ),
          ),
        ],
      ),
    );
  }
}
