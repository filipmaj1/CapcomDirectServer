-- --------------------------------------------------------
-- Host:                         127.0.0.1
-- Server version:               5.6.17 - MySQL Community Server (GPL)
-- Server OS:                    Win64
-- HeidiSQL Version:             10.1.0.5464
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;


-- Dumping database structure for capcomkddi
CREATE DATABASE IF NOT EXISTS `capcomkddi` /*!40100 DEFAULT CHARACTER SET latin1 */;
USE `capcomkddi`;

-- Dumping structure for table capcomkddi.accounts
CREATE TABLE IF NOT EXISTS `accounts` (
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `capcomId` varchar(6) NOT NULL,
  `individualId` varchar(128) NOT NULL,
  `handle` varchar(50) CHARACTER SET sjis DEFAULT NULL,
  `telephone` varchar(128) DEFAULT NULL,
  `email` varchar(320) DEFAULT NULL,
  `name` varchar(128) CHARACTER SET sjis DEFAULT NULL,
  `address` varchar(128) CHARACTER SET sjis DEFAULT NULL,
  `age` smallint(5) unsigned DEFAULT NULL,
  `profession` varchar(128) CHARACTER SET sjis DEFAULT NULL,
  `directMail` tinyint(1) unsigned DEFAULT NULL,
  PRIMARY KEY (`id`,`capcomId`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- Data exporting was unselected.
-- Dumping structure for table capcomkddi.battles
CREATE TABLE IF NOT EXISTS `battles` (
  `battlecode` varchar(14) NOT NULL,
  `player1Id` varchar(6) NOT NULL,
  `player2Id` varchar(6) NOT NULL,
  `createTime` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`battlecode`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE `dialplanservice` (
	`capcomId` VARCHAR(6) NOT NULL,
	`phonenumber` VARCHAR(128) NULL DEFAULT NULL,
	`currentIP` VARCHAR(15) NOT NULL,
	`insertedAt` DATETIME NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
	PRIMARY KEY (`capcomId`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- Data exporting was unselected.
-- Dumping structure for table capcomkddi.gamedata
CREATE TABLE IF NOT EXISTS `gamedata` (
  `capcomId` varchar(6) NOT NULL,
  `gamecode` tinyint(3) unsigned NOT NULL,
  `wins` smallint(5) unsigned NOT NULL DEFAULT '0',
  `losses` smallint(5) unsigned NOT NULL DEFAULT '0',
  `draws` smallint(5) unsigned NOT NULL DEFAULT '0',
  `rank` tinyint(3) unsigned NOT NULL DEFAULT '10',
  `ranking` smallint(5) unsigned NOT NULL DEFAULT '2000',
  `playtime` mediumint(8) unsigned NOT NULL DEFAULT '0',
  `moneyUsed` mediumint(8) unsigned NOT NULL DEFAULT '0',
  PRIMARY KEY (`capcomId`,`gamecode`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- Data exporting was unselected.
-- Dumping structure for table capcomkddi.rooms
CREATE TABLE IF NOT EXISTS `rooms` (
  `gamecode` tinyint(3) unsigned NOT NULL,
  `genrecode` TINYINT(3) unsigned NOT NULL DEFAULT '0',
  `number` smallint(5) unsigned NOT NULL,
  `name` varchar(128) CHARACTER SET sjis DEFAULT NULL,
  `maxUsers` smallint(5) unsigned DEFAULT '50',
  PRIMARY KEY (`gamecode`,`number`, `genrecode`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- Data exporting was unselected.
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IF(@OLD_FOREIGN_KEY_CHECKS IS NULL, 1, @OLD_FOREIGN_KEY_CHECKS) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
