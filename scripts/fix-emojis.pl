#!/usr/bin/perl
use strict;
use warnings;
use utf8;
use Encode qw(decode encode FB_CROAK);

# Enhanced Universal Emoji Fix Script for Markdown Files
# Usage: perl scripts/fix-emojis.pl [-y] <input_file>

# Check command line arguments
my $auto_yes = 0;
my $input_file = '';

if (@ARGV > 2) {
    die "Error: Too many arguments\n";
}

# Parse command line arguments
for my $arg (@ARGV) {
    if ($arg eq '-y') {
        $auto_yes = 1;
    } elsif ($arg =~ /^-/) {
        die "Error: Unknown option '$arg'\n";
    } else {
        $input_file = $arg;
    }
}

if (!$input_file) {
    print "Usage: perl scripts/fix-emojis.pl [-y] <markdown_file>\n";
    print "       -y    Automatically replace original file without prompting\n";
    print "       Fixes both corrupted and proper emoji characters in markdown files\n";
    exit 1;
}

# Validate input file exists
unless (-f $input_file) {
    die "Error: File '$input_file' does not exist.\n";
}

# Create output filename
my $output_file = $input_file;
$output_file =~ s/\.([^.]+)$/_EMOJI_FIXED.$1/; # Add _EMOJI_FIXED before extension

print "Enhanced Universal Emoji Fix Script\n";
print "=" x 50 . "\n";
print "Input file:  $input_file\n";
print "Output file: $output_file\n";
print "Processing file...\n";

# Read file with automatic encoding detection
open(my $scan_fh, '<:raw', $input_file) or die "Cannot open $input_file for scanning: $!";
my $content = do { local $/; <$scan_fh> };
close($scan_fh);

# Try to decode as UTF-8, fall back to raw if it fails
eval {
    $content = decode('UTF-8', $content, FB_CROAK);
};
if ($@) {
    # If UTF-8 decode fails, assume it's already in the right encoding
    # or try Windows-1252
    eval {
        $content = decode('cp1252', $content);
    };
}

my $original_length = length($content);
print "Original file length: $original_length characters\n";

print "Applying emoji fixes...\n";

# Fix common emojis with proper Unicode patterns
# Track replacements manually
my $emoji_count = 0;

$emoji_count += () = $content =~ s/📊/[CHART]/g;
$emoji_count += () = $content =~ s/🚨/[CRITICAL] /g;
$emoji_count += () = $content =~ s/🚀/[ACCELERATION] /g;
$emoji_count += () = $content =~ s/📅/[TIMELINE] /g;
$emoji_count += () = $content =~ s/🎯/[TARGET] /g;
$emoji_count += () = $content =~ s/🎉/[SUCCESS] /g;
$emoji_count += () = $content =~ s/🔧/[TECHNICAL] /g;
$emoji_count += () = $content =~ s/📋/[GUIDE] /g;
$emoji_count += () = $content =~ s/📖/[CRITICAL] /g;
$emoji_count += () = $content =~ s/→/-> /g;
$emoji_count += () = $content =~ s/≤/<= /g;
$emoji_count += () = $content =~ s/✅/[COMPLETE]/g;
$emoji_count += () = $content =~ s/⚠️/[WARNING] /g;
$emoji_count += () = $content =~ s/⚠/[WARNING] /g;
$emoji_count += () = $content =~ s/⚡/[FAST] /g;
$emoji_count += () = $content =~ s/❌/[MISSING]/g;
$emoji_count += () = $content =~ s/✓/[DONE] /g;
$emoji_count += () = $content =~ s/✔/[DONE] /g;
$emoji_count += () = $content =~ s/📝/[NOTE] /g;
$emoji_count += () = $content =~ s/📦/[PACKAGE] /g;
$emoji_count += () = $content =~ s/📄/[DOC] /g;
$emoji_count += () = $content =~ s/📁/[FOLDER] /g;
$emoji_count += () = $content =~ s/🔗/[LINK] /g;
$emoji_count += () = $content =~ s/🛠/[TOOL] /g;
$emoji_count += () = $content =~ s/⚙/[SETTINGS] /g;
$emoji_count += () = $content =~ s/🐛/[BUG] /g;
$emoji_count += () = $content =~ s/🔥/[HOT] /g;
$emoji_count += () = $content =~ s/💡/[IDEA] /g;
$emoji_count += () = $content =~ s/📈/[TRENDING] /g;

print "Emoji replacements made: $emoji_count\n";

# Remove any remaining non-ASCII characters
my $cleaned_content = '';
my $unicode_removed = 0;

for my $i (0 .. length($content) - 1) {
    my $char = substr($content, $i, 1);
    my $ord_val = ord($char);

    if ($ord_val <= 127) {
        # ASCII character - safe to keep
        $cleaned_content .= $char;
    } else {
        # Non-ASCII character - skip it silently (don't add spaces)
        $unicode_removed++;
    }
}

# Clean up formatting issues (minimal intervention)
$cleaned_content =~ s/[ \t]{2,}/ /g;           # Replace multiple spaces/tabs with single space
$cleaned_content =~ s/\*\*\s+\*\*/**/g;        # Fix broken markdown headers
$cleaned_content =~ s/\n{4,}/\n\n\n/g;         # Limit excessive newlines to maximum 3
$cleaned_content =~ s/[ \t]+$//gm;             # Remove trailing spaces/tabs from lines
$cleaned_content =~ s/\n +\n/\n\n/g;           # Remove lines that are just spaces# Write the cleaned content as UTF-8
open(my $out_fh, '>:encoding(UTF-8)', $output_file) or die "Cannot open $output_file for writing: $!";
print $out_fh $cleaned_content;
close($out_fh);

my $final_length = length($cleaned_content);

print "\nProcessing complete!\n";
print "=" x 50 . "\n";
print "Original length:        $original_length characters\n";
print "Final length:           $final_length characters\n";
print "Emoji replacements:     $emoji_count\n";
print "Unicode chars removed:  $unicode_removed\n";
print "Total chars saved:      " . ($original_length - $final_length) . "\n";
print "Output file:            $output_file\n";

# Replace original with cleaned version if user wants
my $replace_file = 0;

if ($auto_yes) {
    $replace_file = 1;
    print "\nAutomatically replacing original file with cleaned version...\n";
} else {
    print "\nReplace original file with cleaned version? (y/N): ";
    my $response = <STDIN>;
    chomp($response);
    $replace_file = (lc($response) eq 'y' || lc($response) eq 'yes');
}

if ($replace_file) {
    rename($output_file, $input_file) or die "Cannot replace original file: $!";
    print "Original file '$input_file' updated with cleaned version.\n";
} else {
    print "Cleaned version saved as '$output_file'.\n";
    print "Original file '$input_file' unchanged.\n";
}

print "Enhanced emoji cleanup completed successfully!\n";
