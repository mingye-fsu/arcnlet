# -*- coding: utf-8 -*-
"""
Author: Wei Mao
Date  : --
Email : weimao@whu.edu.cn
"""

from PySide2.QtWidgets import QApplication, QFileDialog
from PySide2.QtUiTools import QUiLoader
from PySide2.QtCore import QObject
from PySide2.QtGui import QIcon


class ArcNLET(QObject):
    def __init__(self):
        QObject.__init__(self)

        self.ui = QUiLoader().load('ui\\ArcNLET.ui')

        # fileName, _ = QFileDialog.getOpenFileName(self.ui, "Open File")


app = QApplication([])
arcnlet = ArcNLET()
arcnlet.ui.show()
app.exec_()
