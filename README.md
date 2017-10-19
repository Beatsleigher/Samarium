# Samarium

<p align="center" >
  <img alt="Samarium Logo" src="/samarium.png" width="64" height="64" ><br >
  Plugin-based, open (free) email archival written in C#.
</p>

---

# About Samarium

Samarium (named after the lanthanide [Samarium](https://en.wikipedia.org/wiki/Samarium)) is an open (free), plugin-based email archival solution compliant with German data protection laws.

Inspired by a proprietary email archival solution, this software is designed to be open and free (free as in [GNU free](https://www.gnu.org/philosophy/free-sw.en.html)).

Samarium provides a pure backend solution which can be expanded upon, and exposes interfaces for data retrieval and configuration.
Working together with the tried-and-true [Elastic Stack](http://elastic.co) ([GitHub Repository](https://github.com/elastic/elasticsearch/)), Samarium is fully configurable and provides (optionally) encrypted data archival.

# Project Structure

Samarium is fully plugin-based, this includes the plugins used for core functionality.
Each (core) plugin will be contained in a submodule, or a subdirectory in this repository.

To fully clone Samarium and its (core) plugins, make sure to use following command:

```bash
    $ git clone https://github.com/Beatsleigher/Samarium --recursive
```

# Licensing

Samarium is licensed under the BSD 3-clause ("new" or "simplified") license.
For more information, see [LICENSE](/LICENSE)
