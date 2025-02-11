// Used libraries
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace TextEditor {

    // Class for custom commands (add an image, got focus, find text, resize an image),
    // which are handled by functions later in the code
    public static class CustomCommands {
        public static readonly RoutedUICommand AddImageCommand =
            new RoutedUICommand("Add Image", "AddImage", typeof(CustomCommands));

        public static readonly RoutedUICommand GotFocusCommand =
            new RoutedUICommand("Got Focus", "GotFocus", typeof(CustomCommands));

        public static readonly RoutedUICommand FindTextCommand =
            new RoutedUICommand("Find Text", "FindText", typeof(CustomCommands));

        public static readonly RoutedUICommand ResizeCommand =
            new RoutedUICommand("Resize", "ResizeCommand", typeof(CustomCommands));
    }

    public partial class MainWindow : Window {
        // Initialize components,
        // Add options to the font family and size comboboxes
        // Bind custom commands and functions
        public MainWindow() {
            InitializeComponent();

            cmbFontFamily.ItemsSource = Fonts.SystemFontFamilies.OrderBy(f => f.Source);
            cmbFontSize.ItemsSource = new List<double>() { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72 };

            CommandBinding addImageCommandBinding =
                new CommandBinding(CustomCommands.AddImageCommand, AddImageCommand);
            this.CommandBindings.Add(addImageCommandBinding);

            CommandBinding gotFocusCommandBinding =
                new CommandBinding(CustomCommands.GotFocusCommand, GotFocusHandler);
            this.CommandBindings.Add(gotFocusCommandBinding);

            CommandBinding findTextCommandBinding =
                new CommandBinding(CustomCommands.FindTextCommand, FindText);
            this.CommandBindings.Add(findTextCommandBinding);

            CommandBinding resizeCommandBinding =
                new CommandBinding(CustomCommands.ResizeCommand, Resize);
            this.CommandBindings.Add(resizeCommandBinding);
        }
        // Open a file, read the bytes to check extension, read the color bytes and set the combobox (if applicable),
        // load the text to the textbox
        private void Open_Executed(object sender, ExecutedRoutedEventArgs e) {
            OpenFileDialog file = new OpenFileDialog();
            file.Filter = "TextEditor files (*.editor)|*.editor|All files (*.*)|*.*";
            if (file.ShowDialog() == true) {
                using (FileStream fileStream = new FileStream(file.FileName, FileMode.Open)) {
                    byte[] markerBytes = new byte[6];
                    byte[] editorMarkerBytes = new byte[] { 1, 0, 1, 0, 1, 0 };
                    fileStream.Read(markerBytes, 0, markerBytes.Length);
                    if (editorMarkerBytes.SequenceEqual(markerBytes)) {
                        byte[] colorBytes = new byte[3];
                        fileStream.Read(colorBytes, 0, colorBytes.Length);
                        Color backgroundColor = Color.FromRgb(colorBytes[0], colorBytes[1], colorBytes[2]);
                        richTextBox.Background = new SolidColorBrush(backgroundColor);
                        fileStream.Position = 9;
                    }
                    else{
                        fileStream.Position = 0;
                    }
                    richTextBox.Document.Blocks.Clear();
                    using (MemoryStream memoryStream = new MemoryStream()) {
                        fileStream.CopyTo(memoryStream);
                        memoryStream.Position = 0;
                        TextRange range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                        range.Load(memoryStream, DataFormats.Rtf);
                    }
                }

                string fileName = Path.GetFileName(file.FileName);
                FileNameTextBox.Text = fileName;
                if (richTextBox.Background is SolidColorBrush backgroundBrush) {
                    Color currentColor = backgroundBrush.Color;
                    foreach (ComboBoxItem item in cmbBackgroundColor.Items) {
                        if (((SolidColorBrush)item.Background).Color == currentColor) {
                            cmbBackgroundColor.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
        }
        
        // Save file's content and its background color. Handle naming the file according to the file name text box
        private void Save_Executed(object sender, ExecutedRoutedEventArgs e) {
            SaveFileDialog file = new SaveFileDialog();
            file.FileName = FileNameTextBox.Text;
            file.Filter = "TextEditor files (*.editor)|*.editor|All files (*.*)|*.*";
            byte[] editorMarkerBytes = new byte[] { 1, 0, 1, 0, 1, 0 };
            Color backgroundColor = ((SolidColorBrush)richTextBox.Background).Color;
            byte[] colorBytes = new byte[] { backgroundColor.R, backgroundColor.G, backgroundColor.B };
            if (file.ShowDialog() == true) {
                using (FileStream fileStream = new FileStream(file.FileName, FileMode.Create)) {
                    fileStream.Write(editorMarkerBytes, 0, editorMarkerBytes.Length);
                    fileStream.Write(colorBytes, 0, colorBytes.Length);
                    TextRange range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                    range.Save(fileStream, DataFormats.Rtf);
                }
            }
        }

        // Handle focus on different text boxes
        private void GotFocusHandler(object sender, RoutedEventArgs e) {
            if (sender == null)
                return;
            if (sender != FileNameTextBox && FileNameTextBox.Text == "")
                FileNameTextBox.Text = "NewFile";
            if (sender != FindTextBox && FindTextBox.Text == "")
                FindTextBox.Text = "Enter text to find...";
            if (sender == richTextBox) {
                TextRange textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                if (textRange.Text.Trim() == "Start typing a new document or open an existing one")
                    richTextBox.Document.Blocks.Clear();
            }
            else if (sender == FindTextBox)
                FindTextBox.Text = "";
            else if (sender == FileNameTextBox)
                FileNameTextBox.Text = "";
        }

        // Helper function for richTextBox_TextChanged function, handling when selection is empty
        private TextRange findFutureTextRange(RichTextBox richTextBox) {
            TextPointer cursorPosition = richTextBox.CaretPosition;
            TextPointer textEnd = richTextBox.CaretPosition.GetPositionAtOffset(-1, LogicalDirection.Forward);
            if (textEnd == null)
            textEnd = richTextBox.Document.ContentStart;
            return new TextRange(textEnd, cursorPosition);
        }

        // Handle font size, family and color changings, when the text in the rich text box is changed
        private void richTextBox_TextChanged(object sender, TextChangedEventArgs e) {
            if (cmbFontSize.SelectedItem is double selectedFontSize) {
                TextSelection selection = richTextBox.Selection;
                if (selection.IsEmpty) {
                    TextRange futureTextRange = findFutureTextRange(richTextBox);
                    futureTextRange.ApplyPropertyValue(Inline.FontSizeProperty, selectedFontSize);
                }
            }
            if (cmbFontFamily.SelectedItem is FontFamily selectedFontFamily) {
                TextSelection selection = richTextBox.Selection;
                if (selection.IsEmpty) {
                    TextRange futureTextRange = findFutureTextRange(richTextBox);
                    futureTextRange.ApplyPropertyValue(Inline.FontFamilyProperty, selectedFontFamily);
                }
            }
            if (cmbFontColor.SelectedItem is ComboBoxItem selectedFontColor) {
                TextSelection selection = richTextBox.Selection;
                var brush = (SolidColorBrush)selectedFontColor.Background;
                if (selection.IsEmpty) {
                    TextRange futureTextRange = findFutureTextRange(richTextBox);
                    futureTextRange.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
                }
            }
        }

        // Handle toggle buttons changes
        private void richTextBox_SelectionChanged(object sender, RoutedEventArgs e) {
            object temporary = richTextBox.Selection.GetPropertyValue(Inline.FontWeightProperty);
            btnBold.IsChecked = (temporary != DependencyProperty.UnsetValue) && (temporary.Equals(FontWeights.Bold));
            temporary = richTextBox.Selection.GetPropertyValue(Inline.FontStyleProperty);
            btnItalic.IsChecked = (temporary != DependencyProperty.UnsetValue) && (temporary.Equals(FontStyles.Italic));
            temporary = richTextBox.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
            btnUnderline.IsChecked = (temporary != DependencyProperty.UnsetValue) && (temporary.Equals(TextDecorations.Underline));
        }

        // Handle font family changes for the selected text
        private void cmbFontFamily_SelectionChanged(object sender, RoutedEventArgs e) {
            if (cmbFontFamily.SelectedItem is FontFamily selectedFontFamily) {
                TextSelection selection = richTextBox.Selection;
                if (!selection.IsEmpty)
                    selection.ApplyPropertyValue(Inline.FontFamilyProperty, selectedFontFamily);
            }
        }

        // Handle font color changes for the selected text
        private void cmbFontColor_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var item = (ComboBoxItem) cmbFontColor.SelectedItem;
            var brush = (SolidColorBrush) item.Background;
            if (richTextBox != null)
                richTextBox.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
        }

        // Handle font size changes for the selected text
        private void cmbFontSize_SelectionChanged(object sender, RoutedEventArgs e) {
            if (cmbFontSize.SelectedItem is double selectedFontSize) {
                TextSelection selection = richTextBox.Selection;
                if (!selection.IsEmpty)
                    selection.ApplyPropertyValue(Inline.FontSizeProperty, selectedFontSize);
            }
        }

        // Set background color according to the combobox selection
        private void cmbBackgroundColor_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var item = (ComboBoxItem) cmbBackgroundColor.SelectedItem;
            var brush = (SolidColorBrush) item.Background;
            if (richTextBox != null)
                richTextBox.Background = brush;
        }

        // Handle pasting from the clipboard
        private void Paste_Click(object sender, RoutedEventArgs e) => richTextBox.Paste();

        // Handle cutting selected text
        private void Cut_Click(object sender, RoutedEventArgs e) => richTextBox.Cut();

        // Load an image at the position of the cursor
        public void AddImageCommand(object sender, RoutedEventArgs e) {
            OpenFileDialog img = new OpenFileDialog();
            img.Filter = "Image files (*.jpg, *.jpeg, *.gif, *.png) | *.jpg; *.jpeg; *.gif; *.png";
            if (img.ShowDialog() == true) {
                BitmapImage bitmapImage = new BitmapImage(new Uri(img.FileName));
                System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                image.Stretch = Stretch.Fill;
                image.Width = double.Parse(ImageWidthTextBox.Text);
                image.Height = double.Parse(ImageHeightTextBox.Text);
                image.Source = bitmapImage;
                InlineUIContainer container = new InlineUIContainer(image);

                TextPointer insertionPosition = richTextBox.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);
                if (insertionPosition != null)
                    insertionPosition.Paragraph?.Inlines.Add(container);
            }
        }

        // Find the container with an image
        private InlineUIContainer FindInlineUIContainer(TextPointer position) {
            while (position != null && !(position.Parent is InlineUIContainer)) {
                position = position.GetNextContextPosition(LogicalDirection.Backward);
            }
            return position?.Parent as InlineUIContainer;
        }

        // Handle resizing an image
        private void Resize(object sender, RoutedEventArgs e) {
            var selectedPosition = richTextBox.Selection.Start;
            var selectedContainer = FindInlineUIContainer(selectedPosition);
            if (selectedContainer != null && selectedContainer.Child is System.Windows.Controls.Image image) {
                image.Width = double.Parse(ImageWidthTextBox.Text);
                image.Height = double.Parse(ImageHeightTextBox.Text);
            }
        }

        // Get the text from the find text box and find the first occurrence in the text.
        // Handle finding later occurrences by clicking the button
        private TextPointer lastFoundPosition = null;
        private void FindText(object sender, RoutedEventArgs e) {
            string searchText = FindTextBox.Text;
            if (string.IsNullOrEmpty(searchText))
                return;
            TextRange textRange = new TextRange((lastFoundPosition != null)? 
                lastFoundPosition: richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            TextPointer currentPosition = (lastFoundPosition != null)? 
                lastFoundPosition : textRange.Start.GetInsertionPosition(LogicalDirection.Forward);
            while (currentPosition != null) {
                string textInRun = currentPosition.GetTextInRun(LogicalDirection.Forward);
                if (!string.IsNullOrWhiteSpace(textInRun)) {
                    int index = 0;
                    if ((index = textInRun.IndexOf(searchText, index, StringComparison.CurrentCultureIgnoreCase)) != -1) {
                        TextPointer selectionStart = currentPosition.GetPositionAtOffset(index, LogicalDirection.Forward);
                        TextPointer selectionEnd = selectionStart.GetPositionAtOffset(searchText.Length, LogicalDirection.Forward);
                        richTextBox.Selection.Select(selectionStart, selectionEnd);
                        richTextBox.Focus();
                        lastFoundPosition = selectionEnd.GetNextInsertionPosition(LogicalDirection.Forward);
                        return;
                    }
                }
                currentPosition = currentPosition.GetNextContextPosition(LogicalDirection.Forward);
            }
        }
    }
}