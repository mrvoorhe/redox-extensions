use Cwd 'abs_path';
use Getopt::Long;
use File::Spec;
use File::Basename;
use File::Copy;
use File::Path;

my $root = File::Spec->rel2abs(dirname(__FILE__));

my $clean = 0;
my $debug = 0;
my $msBuildVersion = "14.0";
 
GetOptions(
	'clean=i'=>\$clean,
	'debug=i'=>\$debug,
	'msbuildversion=s'=>\$msBuildVersion,
) or die ("illegal cmdline options");

my $sln = "$root/RedoxExtensions.sln";
my $msbuild = $ENV{"ProgramFiles(x86)"}."/MSBuild/$msBuildVersion/Bin/MSBuild.exe";

my $config = $debug ? "Debug" : "Release";
my $target = $clean ? "/t:Clean,Build" :"/t:Build"; 
my $properties = "/p:Configuration=$config;Platform=Any CPU";

print ">>> $msbuild $properties $target $sln\n\n";
system($msbuild, $properties, $target, $sln) eq 0
		or die("MSBuild failed to build $sln\n");
