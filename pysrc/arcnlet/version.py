# -*- coding: utf-8 -*-

"""
arcnlet version file

Author: Wei Mao
Date  : October 15, 2022 16:33:03
Email : weimao@whu.edu.cn
"""

major = 1
minor = 0
micro = 0
__version__ = f"{major}.{minor}.{micro}"

__pakname__ = "arcnlet"

author_dict = {
    "Wei Mao": "weimao@whu.edu.cn"
}

__author__ = ", ".join(author_dict.keys())
__author_email__ = ", ".join(s for _, s in author_dict.items())
