# <img width="24px" src="./Logo/256.png" alt="Sonarr"></img> Sonarr

[![Translated](https://translate.servarr.com/widget/servarr/sonarr/svg-badge.svg)](https://translate.servarr.com/engage/servarr/)
[![Backers on Open Collective](https://opencollective.com/Sonarr/backers/badge.svg)](#backers)
[![Sponsors on Open Collective](https://opencollective.com/Sonarr/sponsors/badge.svg)](#sponsors)
[![Mega Sponsors on Open Collective](https://opencollective.com/Sonarr/megasponsors/badge.svg)](#mega-sponsors)

Sonarr is a PVR for Usenet and BitTorrent users. It can monitor multiple RSS feeds for new episodes of your favorite shows and will grab, sort and rename them. It can also be configured to automatically upgrade the quality of files already downloaded when a better quality format becomes available.

## lasyan3's fork

This fork follows the official Sonarr releases and adds a few changes on top of them. They are also listed in the app under System → Status → About ("lasyan3's addons").

### Addons

#### Season search

Sonarr searches a season differently depending on the series type. This fork makes Standard and Anime series behave the same way: search the season pack, and only search episodes one by one while the season is still airing. Daily series are unchanged.

- **Standard series**: when the season has not fully aired, also searches the aired episodes one by one.
  _Why_: Sonarr only searches the season pack, which does not exist yet for a season that is still airing, so nothing is found.
- **Anime series**: only searches episodes one by one for the last season of an anime that is still airing.
  _Why_: Sonarr searches every episode individually after each anime season search. For a finished season, packs exist and these extra searches mostly flood the indexers (and hit their API limits) for nothing.
- **Anime series**: for an ended anime with a single season, searches without the season number.
  _Why_: such anime are usually released without any season number (e.g. `Gungrave 01-26`), so a search restricted to season 1 finds nothing.

#### Other

- **Anime episode search**: in addition to `One Piece 1152`, also searches `One Piece E1152`.
  _Why_: some releases name anime episodes with an `E` prefix before the absolute number, and the indexers' text search does not find them with the number alone.
- **Alternate titles**: add, hide or restore the alternate titles of a series from the series page.
  _Why_: alternate titles come from scene mappings and are used both to build searches and to match releases. A missing title (e.g. a local title) means releases are never found, and an irrelevant one leads to useless searches or wrong matches. Sonarr does not let you change them.
- **Manual import**: ignores symbolic links when scanning a folder.
  _Why_: symbolic links point to files that already exist elsewhere (e.g. already imported or managed by another tool), so they show up as duplicates in the manual import list.

### Docker image

The `Docker (lasyan3)` workflow builds an image based on [linuxserver/docker-sonarr](https://github.com/linuxserver/docker-sonarr) and publishes it as `ghcr.io/lasyan3/sonarr`:

- a pull request to `develop` is built to validate it, nothing is published;
- a merged pull request is built, published (`latest` and `<official version>-lasyan3.<run>`) and deployed to Unraid;
- direct pushes to `develop` do not trigger anything.

The app reports the official version it is based on (e.g. `4.0.20.3014`), so Sonarr still detects new official releases.

## Getting Started

- [Download/Installation](https://sonarr.tv/#downloads-v3)
- [FAQ](https://wiki.servarr.com/sonarr/faq)
- [Wiki](https://wiki.servarr.com/Sonarr)
- [v4 Beta API Documentation](https://sonarr.tv/docs/api)
- [Donate](https://sonarr.tv/donate)

## Support

Note: GitHub Issues are for Bugs and Feature Requests Only

- [Forums](https://forums.sonarr.tv/)
- [Discord](https://discord.gg/M6BvZn5)
- [GitHub - Bugs and Feature Requests Only](https://github.com/Sonarr/Sonarr/issues)
- [IRC](https://web.libera.chat/?channels=#sonarr)
- [Reddit](https://www.reddit.com/r/sonarr)
- [Wiki](https://wiki.servarr.com/sonarr)

## Features

### Current Features

- Support for major platforms: Windows, Linux, macOS, Raspberry Pi, etc.
- Automatically detects new episodes
- Can scan your existing library and download any missing episodes
- Can watch for better quality of the episodes you already have and do an automatic upgrade. _eg. from DVD to Blu-Ray_
- Automatic failed download handling will try another release if one fails
- Manual search so you can pick any release or to see why a release was not downloaded automatically
- Fully configurable episode renaming
- Full integration with SABnzbd and NZBGet
- Full integration with Kodi, Plex (notification, library update, metadata)
- Full support for specials and multi-episode releases
- And a beautiful UI

## Contributing

### Development

This project exists thanks to all the people who contribute. [Contribute](CONTRIBUTING.md).

<a href="https://github.com/Sonarr/Sonarr/graphs/contributors"><img src="https://opencollective.com/Sonarr/contributors.svg?width=890&button=false" /></a>

### Supporters

This project would not be possible without the support of our users and software providers.
[**Become a sponsor or backer**](https://opencollective.com/sonarr) to help us out!

#### Mega Sponsors

[![Sponsors](https://opencollective.com/sonarr/tiers/mega-sponsor.svg?width=890)](https://opencollective.com/sonarr/contribute/mega-sponsor-21443/checkout)

#### Sponsors

[![Flexible Sponsors](https://opencollective.com/sonarr/sponsors.svg?width=890)](https://opencollective.com/sonarr/contribute/sponsor-21457/checkout)

#### Backers

[![Backers](https://opencollective.com/sonarr/backers.svg?width=890)](https://opencollective.com/sonarr/contribute/backer-21442/checkout)

#### JetBrains

Thank you to [<img src="https://resources.jetbrains.com/storage/products/company/brand/logos/jetbrains.png" alt="JetBrains" width="96">](http://www.jetbrains.com/) for providing us with free licenses to their great tools

[<img src="https://resources.jetbrains.com/storage/products/company/brand/logos/TeamCity.png" alt="TeamCity" width="64">](http://www.jetbrains.com/teamcity/)

[<img src="https://resources.jetbrains.com/storage/products/company/brand/logos/ReSharper.png" alt="ReSharper" width="64">](http://www.jetbrains.com/resharper/)

[<img src="https://resources.jetbrains.com/storage/products/company/brand/logos/dotTrace.png" alt="dotTrace" width="64">](http://www.jetbrains.com/dottrace/)

[<img src="https://resources.jetbrains.com/storage/products/company/brand/logos/Rider.png" alt="Rider" width="64">](http://www.jetbrains.com/rider/)

### Licenses

- [GNU GPL v3](http://www.gnu.org/licenses/gpl.html)
- Copyright 2010-2024
