# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

## [0.1.0] - 2024-07-05

- First testing release, supports stamps

## [0.2.0] - 2025-10-06

- First fully functional release. Supports stamps and items.

## [0.3.0] - 2025-10-12

- Added achievements and cassettes
- Added options to change stamp requirements per region
- Lots of bug fixes

## [1.0.0] - 2025-10-24

- Official release!
- Overhauled UI
    - Added a command input field
    - Improved focus handling (game no longer moves when processing input field typing)
    - Prevent starting game when mouse down on the Archipelago UIs
    - Pull up new game menu on client connection
- Play sounds when awarding items from server

## [1.0.1] - 2025-10-25

- Bug fix to allow the include_basto option to be set to false

## [1.0.2] - 2025-11-22

- Bug fix prevent the show/hide console log button from retaining focus
- Removed skip balancing from stamp items

## [1.1.0] - 2025-11-29

- Add option to make stamps progressive. Option set by default to encourage better default balancing.

## [1.1.1] - 2025-12-19

- Bug fix where `progressive_stamp_requirements` was not instanced per world (and so all slots would share the same one)
- Switched to building the .apworld with the "Build APWorlds" command in the launcher
- Added cooldown on Stamp/Photo/Item get sounds

## [1.9.0] - 2026-02-01

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

## [2.0.0] - 2026-02-26

- Reimplments Cassettes with new ER logic
- Added version checks for Client and UT
- Adjust min connection window size for lower resolutions
- Make ER data more memory efficient
- Bugfix: Cassettes locations trigger properly if item is already received
- Bugfix: Added logic for raft being fixed with electricity
- Bugfix: Basto ticket now properly unlocks house exit

## [2.0.1] - 2026-03-01

- Client version check now allows any patch version (aka, the 2.0.1 will work on any apworld with version 2.0.x)
- Bugfix: Basto disabled option works again (was giving key error on gen)
- Bugfix: Fixed bad entrance logic when progressive stamps are off

## [2.0.2] - 2026-03-02

- No client change from 2.0.1 (so you don't need to update client if already on 2.0.1)
- Bugfix: Fix key error when enforce_deferred_connections is on
- Bugfix: Fix type error when enforce_deferred_connections is off

## [2.0.3] - 2026-03-22

- No apworld change from 2.0.2 (so you don't need to update apworld if already on 2.0.2)
- Bugfix: Make TraversedEntrances in DataStorage updated async. Might fix the ER transition breaking bug
- Bugfix: Have Basto item checks not use IsLocationChecked when Basto is disabled
- Bugfix: Allow checking the water popper location if gate is already opened

## [2.0.4] - 2026-04-24

- Bugfix: Fix a regression in deferred entrances
- Bugfix: Correct a logic bug with the Sandwich item's location

## [2.0.5] - 2026-04-25

- Bugfix: Prevent occasionally generating an impossible Basto ER

## [2.0.6] - 2026-04-27

- Bugfix: Fix bad new version usage preventing connect

## [2.1.0] - 2026-05-

- Raft and ski lift transitions are now randomized