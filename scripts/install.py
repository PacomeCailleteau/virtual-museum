import argparse
import json
from pathlib import Path, PurePosixPath
import shutil
import tempfile
import zipfile


ROOT = Path(__file__).resolve().parents[1]
SCENE = Path("Assets/Scenes/VirtualMuseum.unity")
MANIFEST = Path("Packages/manifest.json")


def unpack_starter(archive, project):
    if project.exists():
        raise ValueError(f"Le dossier existe sans la scène du socle Unity : {project}")

    with tempfile.TemporaryDirectory(dir=project.parent) as temporary:
        staging = Path(temporary)
        with zipfile.ZipFile(archive) as source:
            scenes = []
            for member in source.infolist():
                path = PurePosixPath(member.filename)
                if path.is_absolute() or ".." in path.parts or "\\" in member.filename or ":" in member.filename:
                    raise ValueError(f"Chemin invalide dans l’archive : {member.filename}")
                if path.as_posix().endswith(SCENE.as_posix()):
                    scenes.append(path.parents[2])
            if len(scenes) != 1:
                raise ValueError("L’archive doit contenir exactement un projet Unity Virtual Museum.")
            source.extractall(staging)

        starter = staging / scenes[0]
        if not (starter / MANIFEST).is_file():
            raise ValueError("Le manifeste Unity est absent du projet de départ.")
        if starter == staging:
            project.mkdir()
            for entry in staging.iterdir():
                entry.replace(project / entry.name)
        else:
            starter.replace(project)


def install_local_packages(project, archives):
    manifest_path = project / MANIFEST
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    dependencies = manifest["dependencies"]
    lock = json.loads((manifest_path.parent / "packages-lock.json").read_text(encoding="utf-8"))
    local = manifest_path.parent / "TeamLocal"
    for package, info in lock["dependencies"].items():
        archive = archives / f"{package}-{info['version']}.tgz"
        if not archive.is_file():
            continue
        local.mkdir(exist_ok=True)
        installed = local / archive.name
        shutil.copy2(archive, installed)
        dependencies[package] = "file:TeamLocal/" + installed.name
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description="Installer Virtual Museum sur le socle Unity de l’IMT.")
    parser.add_argument("archive", type=Path, help="Archive ZIP du socle GitLab IMT.")
    parser.add_argument("--project", type=Path, default=ROOT / "Unity", help="Dossier du projet Unity.")
    parser.add_argument("--package-archives", type=Path, help="Dossier de fichiers .tgz Unity déjà téléchargés.")
    args = parser.parse_args()
    project = args.project.resolve()
    try:
        if not (project / SCENE).is_file():
            project.parent.mkdir(parents=True, exist_ok=True)
            unpack_starter(args.archive, project)
        assets = project / "Assets"
        shutil.copytree(ROOT / "Assets/CampusMuseum", assets / "CampusMuseum", dirs_exist_ok=True)
        shutil.copy2(ROOT / "Assets/CampusMuseum.meta", assets)
        if args.package_archives:
            install_local_packages(project, args.package_archives.resolve())
    except (OSError, ValueError, zipfile.BadZipFile) as error:
        parser.exit(1, f"Installation arrêtée. {error}\n")
    print(f"Projet Unity prêt dans {project}")


if __name__ == "__main__":
    main()
