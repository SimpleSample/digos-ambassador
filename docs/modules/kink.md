﻿Kink Commands
=============
## Summary
These commands are prefixed with `kink`. 

Commands for viewing and configuring user kinks.

## Commands
### *info*
#### Overloads
**`kink info`**

Shows information about the named kink.

| Name | Type | Optional |
| --- | --- | --- |
| name | string | `no` |

---

### *show*
#### Overloads
**`kink show` (or `kink preference`)**

Shows your preference for the named kink.

| Name | Type | Optional |
| --- | --- | --- |
| name | string | `no` |

**`kink show` (or `kink preference`)**

Shows the user's preference for the named kink.

| Name | Type | Optional |
| --- | --- | --- |
| user | IUser | `no` |
| name | string | `no` |

---

### *overlap*
#### Overloads
**`kink overlap`**

Shows the kinks which overlap between you and the given user.

| Name | Type | Optional |
| --- | --- | --- |
| otherUser | IUser | `no` |

---

### *by-preference*
#### Overloads
**`kink by-preference`**

Shows your kinks with the given preference.

| Name | Type | Optional |
| --- | --- | --- |
| preference | KinkPreference | `no` |

**`kink by-preference`**

Shows the given user's kinks with the given preference.

| Name | Type | Optional |
| --- | --- | --- |
| otherUser | IUser | `no` |
| preference | KinkPreference | `no` |

---

### *preference*
#### Overloads
**`kink preference`**

Sets your preference for the given kink.

| Name | Type | Optional |
| --- | --- | --- |
| name | string | `no` |
| preference | KinkPreference | `no` |

---

### *wizard*
#### Overloads
**`kink wizard`**

Runs an interactive wizard for setting kink preferences.

---

### *update-db*
#### Overloads
**`kink update-db`**

Updates the kink database with data from F-list.

---

### *reset*
#### Overloads
**`kink reset`**

Resets all your kink preferences.

<sub><sup>Generated by DIGOS.Ambassador.Doc</sup></sub>