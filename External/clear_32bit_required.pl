use strict;
use warnings;

# Clears the 32BITREQUIRED flag in a managed assembly's CLR header so it can be
# loaded by a 64-bit process.  Only used on the compile-time copies of Decal and
# Virindi assemblies so the tests can run on 64-bit .NET.

die("usage: perl $0 <dll>...\n") unless @ARGV;

foreach my $path (@ARGV) {
	open(my $fh, '+<:raw', $path) or die("cannot open $path: $!\n");
	local $/;
	my $data = <$fh>;

	my $pe = unpack('V', substr($data, 0x3c, 4));
	die("$path: not a PE file\n") unless substr($data, $pe, 4) eq "PE\0\0";

	my $sectionCount = unpack('v', substr($data, $pe + 6, 2));
	my $optionalHeaderSize = unpack('v', substr($data, $pe + 20, 2));
	my $optionalHeader = $pe + 24;
	my $magic = unpack('v', substr($data, $optionalHeader, 2));
	my $dataDirectories = $optionalHeader + ($magic == 0x10b ? 96 : 112);
	my $clrHeaderRva = unpack('V', substr($data, $dataDirectories + 14 * 8, 4));
	die("$path: not a managed assembly\n") unless $clrHeaderRva;

	my $flagsOffset;
	my $sections = $optionalHeader + $optionalHeaderSize;
	for my $i (0 .. $sectionCount - 1) {
		my ($virtualSize, $virtualAddress, undef, $rawPointer) = unpack('VVVV', substr($data, $sections + 40 * $i + 8, 16));
		if ($clrHeaderRva >= $virtualAddress && $clrHeaderRva < $virtualAddress + $virtualSize) {
			$flagsOffset = $clrHeaderRva - $virtualAddress + $rawPointer + 16;
			last;
		}
	}
	die("$path: CLR header not found in any section\n") unless defined $flagsOffset;

	my $flags = unpack('V', substr($data, $flagsOffset, 4));
	my $newFlags = $flags & ~0x2;
	if ($newFlags == $flags) {
		printf("%s: already clear (flags 0x%x)\n", $path, $flags);
	} else {
		seek($fh, $flagsOffset, 0) or die("$path: seek failed: $!\n");
		print $fh pack('V', $newFlags);
		printf("%s: flags 0x%x -> 0x%x\n", $path, $flags, $newFlags);
	}
	close($fh) or die("$path: close failed: $!\n");
}
