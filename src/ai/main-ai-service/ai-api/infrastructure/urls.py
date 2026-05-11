from urllib.parse import urljoin


def join_base_url(base_url: str, path_or_url: str) -> str:
    if path_or_url.startswith("http://") or path_or_url.startswith("https://"):
        return path_or_url

    base = base_url.rstrip("/") + "/"
    path = path_or_url.lstrip("/")
    return urljoin(base, path)