use Cwd 'abs_path';
use Getopt::Long;
use File::Spec;
use File::Basename;
use File::Copy;
use File::Path;

my $root = File::Spec->rel2abs(dirname(__FILE__));

my $clean = 0;
my $msBuildVersion = "14.0";
 
GetOptions(
	'clean=i'=>\$clean,
	'msbuildversion=s'=>\$msBuildVersion,
) or die ("illegal cmdline options");

system("$root/build.pl", "--debug=1", "--clean=$clean") eq 0 or die("failed to build\n");

my $sln = "$root/RedoxExtensions.sln";
my $nunit = "$root/External/NUnit/bin/nunit-console-x86.exe";

print ">>> $nunit $sln\n\n";
system($nunit, "--labels", $sln) eq 0 or die("Tests failed\n");
