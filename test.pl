use File::Spec;
use File::Basename;

my $root = File::Spec->rel2abs(dirname(__FILE__));
my $sln = "$root/RedoxExtensions.slnx";

print ">>> dotnet test $sln\n\n";
system("dotnet", "test", $sln, @ARGV) eq 0 or die("Tests failed\n");
