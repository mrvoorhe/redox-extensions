"""Call the agent bridge CLI (rectl) from Python and get JSON back.

Runs the built rectl.dll with dotnet directly, not rectl.cmd: cmd treats "|" in a --wait regex as a pipe.
"""
import json, os, subprocess

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
CLI_PROJECT = os.path.join(REPO, 'RedoxExtensions.AgentCli')
DLL = os.path.join(CLI_PROJECT, 'bin', 'Debug', 'net10.0', 'rectl.dll')


class RectlError(Exception):
    pass


def ensure_built():
    if not os.path.exists(DLL):
        subprocess.run(['dotnet', 'build', CLI_PROJECT, '-c', 'Debug', '-v', 'q', '-nologo'], check=True,
                       capture_output=True)


def run(char, *args, check=True):
    """Runs `rectl -c <char> <args>` and returns the parsed JSON (None if it printed nothing)."""
    ensure_built()
    p = subprocess.run(['dotnet', DLL, '-c', char, *map(str, args)], capture_output=True, text=True)
    if check and p.returncode not in (0,):
        raise RectlError(f'rectl {" ".join(map(str, args))} exited {p.returncode}: {p.stderr.strip() or p.stdout.strip()}')
    return json.loads(p.stdout) if p.stdout.strip() else None


def state(char):
    return run(char, 'state')


def exec_wait(char, command, wait_regex, kinds='chat,plugin', timeout_ms=5000):
    """Runs an in-game command and waits for a line matching wait_regex.  Returns the matched text or None."""
    r = run(char, 'exec', command, '--wait', wait_regex, '--kinds', kinds, '--timeout', timeout_ms, check=False)
    wait = (r or {}).get('wait') or {}
    return (wait.get('event') or {}).get('text') if wait.get('matched') else None


def vt_option(char, name):
    """Reads a VT option with /vt opt get.  Returns the value text, or None if VT didn't answer."""
    text = exec_wait(char, f'/vt opt get {name}', f'Option {name} =')
    return text.split('=', 1)[1].strip() if text and '=' in text else None


def landcell(s):
    """Landcell from a state response as an unsigned int (the bridge reports it signed)."""
    return s['location']['landcell'] & 0xFFFFFFFF


def vt_dir():
    """The user's VirindiTank folder (navs, metas, profiles): VT_DIR, else what `rectl paths` reports."""
    if os.environ.get('VT_DIR'):
        return os.environ['VT_DIR']
    ensure_built()
    p = subprocess.run(['dotnet', DLL, 'paths'], capture_output=True, text=True)
    vt = json.loads(p.stdout).get('vt') if p.stdout.strip() else None
    if not vt:
        raise RectlError('VirindiTank folder not found: set VT_DIR or REDOX_EXTENSIONS_INSTALL')
    return vt


if __name__ == '__main__':
    import sys
    if sys.argv[1:] == ['vt-dir']:
        print(vt_dir())
    else:
        print('usage: py tools/vt/rectl.py vt-dir')
        sys.exit(2)
