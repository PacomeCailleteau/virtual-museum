import argparse
import json
from pathlib import Path, PurePosixPath
import re
import shutil
import tempfile
import zipfile


ROOT = Path(__file__).resolve().parents[1]
SCENE = Path("Assets/Scenes/VirtualMuseum.unity")
MANIFEST = Path("Packages/manifest.json")
WORKSHOP = Path("Assets/CampusMuseum/Resources/CampusMuseum/Workshop.prefab.meta")


def add_workshop_to_scene(project):
    scene_path = project / SCENE
    scene = scene_path.read_bytes()
    prefab_meta = (project / WORKSHOP).read_text(encoding="utf-8")
    guid = re.search(r"^guid: ([a-f0-9]{32})$", prefab_meta, re.MULTILINE)
    if not guid:
        raise ValueError("Le prefab de l’atelier est introuvable.")
    reference = f"  m_SourcePrefab: {{fileID: 100100000, guid: {guid[1]}, type: 3}}".encode()
    if reference in scene:
        return

    roots = re.search(rb"^--- !u!1660057539 &\d+\r?\nSceneRoots:\r?\n", scene, re.MULTILINE)
    if not roots or not re.fullmatch(rb"(?s)SceneRoots:\s+m_ObjectHideFlags: 0\s+m_Roots:\s*(?:- \{fileID: -?\d+\}\s*)+", scene[roots.start():].split(b"\n", 1)[1]):
        raise ValueError("La liste des objets racine de VirtualMuseum.unity est introuvable.")
    existing_ids = set(re.findall(rb"^--- !u!\d+ &(-?\d+)", scene, re.MULTILINE))
    instance_id = 903610200000000001
    while str(instance_id).encode() in existing_ids:
        instance_id += 1
    newline = b"\r\n" if b"\r\n" in scene else b"\n"
    instance = [
        f"--- !u!1001 &{instance_id}",
        "PrefabInstance:",
        "  m_ObjectHideFlags: 0",
        "  serializedVersion: 2",
        "  m_Modification:",
        "    serializedVersion: 3",
        "    m_TransformParent: {fileID: 0}",
        "    m_Modifications: []",
        "    m_RemovedComponents: []",
        "    m_RemovedGameObjects: []",
        "    m_AddedGameObjects: []",
        "    m_AddedComponents: []",
        reference.decode(),
    ]
    with tempfile.NamedTemporaryFile(dir=scene_path.parent, delete=False) as output:
        output.write(scene[:roots.start()])
        output.write(newline.join(line.encode() for line in instance) + newline)
        output.write(scene[roots.start():].rstrip(b"\r\n") + newline)
        output.write(f"  - {{fileID: {instance_id}}}".encode() + newline)
    temporary = Path(output.name)
    try:
        temporary.replace(scene_path)
    finally:
        temporary.unlink(missing_ok=True)


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
        add_workshop_to_scene(project)
        if args.package_archives:
            install_local_packages(project, args.package_archives.resolve())
    except (OSError, ValueError, zipfile.BadZipFile) as error:
        parser.exit(1, f"Installation arrêtée. {error}\n")
    print(f"Projet Unity prêt dans {project}")


if __name__ == "__main__":
    main()
