﻿<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
  <xsl:output method="xml" indent="yes"/>

  <!-- Set up a local connection string -->
  <xsl:template match="/configuration/appSettings/add[@key='umbracoDbDSN']/@value">
    <xsl:attribute name="value">server=.\sqlexpress;database=umbraco-4.1;Integrated Security=true</xsl:attribute>
  </xsl:template>
  
  <!-- Default templates to match anything else -->
  <xsl:template match="@*">
    <xsl:copy/>
  </xsl:template>

  <xsl:template match="node()">
    <xsl:copy>
      <xsl:apply-templates select="@*"/>
      <xsl:apply-templates/>
    </xsl:copy>
  </xsl:template>
</xsl:stylesheet>
