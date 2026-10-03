// Smoke test for the hello_flutter sample app.

import 'package:flutter_test/flutter_test.dart';

import 'package:hello_flutter/main.dart';

void main() {
  testWidgets('HelloApp shows Hello, World!', (WidgetTester tester) async {
    await tester.pumpWidget(const HelloApp());

    expect(find.text('Hello, World!'), findsOneWidget);
  });
}
