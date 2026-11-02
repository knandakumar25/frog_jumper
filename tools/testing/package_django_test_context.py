"""Bundle the root POSIX virtual environment and backend source for Windows-hosted Docker test execution."""
import argparse
import os
import tarfile
from pathlib import Path


def add_regular_tree(archive, root, relative, excluded_parts):
    """Add directories and regular files while omitting symlinks and local runtime caches."""
    source = root / relative
    for path in [source, *source.rglob('*')]:
        rel = path.relative_to(root)
        if any(part in excluded_parts for part in rel.parts):
            continue
        if path.is_symlink():
            continue
        if path.is_dir():
            archive.add(path, arcname=rel.as_posix(), recursive=False)
        elif path.is_file() and path.suffix != '.pyc':
            archive.add(path, arcname=rel.as_posix(), recursive=False)


def create_bundle(project_root, destination):
    """Create a compressed, secret-free test context containing only `.venv` and backend source."""
    excluded = {'__pycache__', '.pytest_cache', '.mypy_cache', '.ruff_cache', '.git', '.env', '.venv'}
    with tarfile.open(destination, mode='w:gz', format=tarfile.PAX_FORMAT) as archive:
        add_regular_tree(archive, project_root, Path('.venv'), excluded - {'.venv'})
        add_regular_tree(archive, project_root, Path('backend'), excluded)


def main():
    """Write a one-run tarball for transfer into the existing unmounted backend container."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    root = args.root.resolve()
    django_package = root / '.venv/lib/python3.14/site-packages/django/__init__.py'
    if not django_package.is_file() or not (root / '.venv/pyvenv.cfg').is_file() or not (root / 'backend/manage.py').is_file():
        raise SystemExit('Root POSIX venv or backend/manage.py is missing.')
    create_bundle(root, args.output.resolve())
    print(f'Prepared root-venv Django test context ({args.output.stat().st_size} bytes).')


if __name__ == '__main__':
    main()
