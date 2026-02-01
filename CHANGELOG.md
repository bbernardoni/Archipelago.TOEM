# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [0.1.0] - 2024-07-05

- First testing release, supports stamps

## [0.2.0] - 2024-10-06

- First fully functional release. Supports stamps and items.

## [0.3.0] - 2024-10-12

- Added achievements and cassettes
- Added options to change stamp requirements per region
- Lots of bug fixes

## [1.0.0] - 2024-10-24

- Official release!
- Overhauled UI
    - Added a command input field
    - Improved focus handling (game no longer moves when processing input field typing)
    - Prevent starting game when mouse down on the Archipelago UIs
    - Pull up new game menu on client connection
- Play sounds when awarding items from server

## [1.0.1] - 2024-10-25

- Bug fix to allow the include_basto option to be set to false

## [1.0.2] - 2024-11-22

- Bug fix prevent the show/hide console log button from retaining focus
- Removed skip balancing from stamp items

## [1.1.0] - 2024-11-29

- Add option to make stamps progressive. Option set by default to encourage better default balancing.

## [1.1.1] - 2024-12-19

- Bug fix where `progressive_stamp_requirements` was not instanced per world (and so all slots would share the same one)
- Switched to building the .apworld with the "Build APWorlds" command in the launcher
- Added cooldown on Stamp/Photo/Item get sounds

## [1.9.0] - 2024-02-01

- Entrance Randomizer beta!
    - Adds one ER mode of randomizing entrances within each region
    - Most room transitions are randomized (exceptions are Stanhamn raft and Kiiruberg ski lift)
- Implemented basic UT integration
    - Allows yamless support
    - Supports deferred entrances
- Randomizes Basto express ticket which allows for reaching Basto earlier
- Removes randomizing cassettes as they are not ready for the beta
- Bugfix: Many logic bugs corrected in ER logic overhaul
- Bugfix: Fix initial connection to server where some items have already been received
- Bugfix: Trading hats with the pirate queen no longer takes away your old hat
- Bugfix: Turning in multiple fruits to the ice cream vendor awards all locations not just the last one
- Bugfix: Punky parrot location check now functional