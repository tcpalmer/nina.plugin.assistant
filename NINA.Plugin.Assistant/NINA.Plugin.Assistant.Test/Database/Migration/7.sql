/*
    Copyright © 2016 - 2022 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors 

    This file is part of N.I.N.A. - Nighttime Imaging N Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

/*
 * Corrections to existing entries
 */

/* The Vorontsov-Vel'yaminov catalog is abbreviated "VV", not "V V" */
UPDATE cataloguenr SET
    catalogue = 'VV'
    WHERE catalogue = 'V V';
    
/* IC63 is also LBN 622, not LBN 623 */
UPDATE cataloguenr SET
    designation = '622'
    WHERE catalogue = 'LBN' AND designation = '623' AND dsodetailid = 'IC63';

/* NGC7635 is also LBN 548, not LBN 549 */
UPDATE cataloguenr SET
    designation = '548'
    WHERE catalogue = 'LBN' AND designation = '549' AND dsodetailid = 'NGC7635';

/* Sh2-132 is also LBN 473, not LBN 470 */
UPDATE cataloguenr SET
    designation = '473'
    WHERE catalogue = 'LBN' AND designation = '470' AND dsodetailid = 'Sh2-132';

/* IC448 is also LBN 931, not LBN 930 */
UPDATE cataloguenr SET
    designation = '931'
    WHERE catalogue = 'LBN' AND designation = '930' AND dsodetailid = 'IC448';

/* IC4604 is also LBN 1111, not LBN 1112 */
UPDATE cataloguenr SET
    designation = '1111'
    WHERE catalogue = 'LBN' AND designation = '1112' AND dsodetailid = 'IC4604';

/* Sh2-263 is also LBN 867, not LBN 866 */
UPDATE cataloguenr SET
    designation = '867'
    WHERE catalogue = 'LBN' AND designation = '866' AND dsodetailid = 'Sh2-263';

/* IC2162 is also Sh 2-255, not Sh 2-256 */
UPDATE cataloguenr SET
    designation = '255'
    WHERE catalogue = 'Sh2' AND designation = '256' AND dsodetailid = 'IC2162';

/* Sh2-55 is also LBN 73, not LBN 74 */
UPDATE cataloguenr SET
    designation = '73'
    WHERE catalogue = 'LBN' AND designation = '74' AND dsodetailid = 'Sh2-55';

/* NGC6559 is also LBN 29, not LBN 28 */
UPDATE cataloguenr SET
    designation = '29'
    WHERE catalogue = 'LBN' AND designation = '28' AND dsodetailid = 'NGC6559';

/* NGC6590 is also LBN 46, not LBN 43 */
UPDATE cataloguenr SET
    designation = '46'
    WHERE catalogue = 'LBN' AND designation = '43' AND dsodetailid = 'NGC6590';

/* NGC1432 is also LBN 771, not LBN 772 */
UPDATE cataloguenr SET
    designation = '771'
    WHERE catalogue = 'LBN' AND designation = '772' AND dsodetailid = 'NGC1432';

/* Sh2-88 is also LBN 139, not LBN 149 */
UPDATE cataloguenr SET
    designation = '139'
    WHERE catalogue = 'LBN' AND designation = '149' AND dsodetailid = 'Sh2-88';

/* IC63 is also LBN 622, not LBN 623 */
UPDATE cataloguenr SET
    designation = '622'
    WHERE catalogue = 'LBN' AND designation = '623' AND dsodetailid = 'IC63';

/* VDB 8 is not LBN 643 */
DELETE FROM cataloguenr
    WHERE dsodetailid = 'vdB8' AND catalogue = 'LBN' AND designation = '643';


/* NGC6523 is a star cluster within M8, not M8 itself */
UPDATE dsodetail SET
    notes = 'Bipolar nebula in M 8 (Lagoon nebula), associated to the O-star Herschel 36',
    lastmodified = '2022-06-20 04:11:54'
    WHERE id = 'NGC6523';

DELETE FROM cataloguenr
    WHERE dsodetailid = 'NGC6523' AND catalogue = 'M' AND designation = '8';
DELETE FROM cataloguenr
    WHERE dsodetailid = 'NGC6523' AND catalogue = 'NAME' AND designation = 'Lagoon Nebula';

/*
 * NGC6533 is equivalent to M8 according to ViZer and online resources.
 * Touch up the coordinates (from simbad) and associate other popular designations with it
 */
UPDATE dsodetail SET
    ra = '270.90416666666664',
    dec = '-24.386666666666667',
    magnitude = '4.6',
    surfacebrightness = '5.8',
    notes = 'M 8 contains NGC 6523 and NGC 6530',
    lastmodified = '2022-06-20 04:11:54'
    WHERE id = 'NGC6533';
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6533', 'M', '8');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6533', 'NAME', 'Lagoon Nebula');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6533', 'Sh2', '25');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6533', 'LBN', '25');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6533', 'Gum', '72');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6533', 'RCW', '126');

/* Missing popular designations for existing database objects */
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6302', 'Sh2', '6');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6302', 'Gum', '60');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6302', 'RCW', '124');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6357', 'Sh2', '11');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6357', 'Gum', '66');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6357', 'RCW', '131');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC410', 'LBN', '807');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC417', 'Sh2', '234');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC1931', 'LBN', '810');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC1931', 'Sh2', '237');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-205', 'LBN', '701');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC10', 'LBN', '591');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC281', 'LBN', '616');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC281', 'Sh2', '184');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC1805', 'LBN', '654');
INSERT OR REPLACE INTO cataloguenr VALUES ('IC1805', 'Sh2', '190');
INSERT OR REPLACE INTO cataloguenr VALUES ('IC1805', 'Collinder', '26');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC1848', 'LBN', '667');
INSERT OR REPLACE INTO cataloguenr VALUES ('IC1848', 'Sh2', '199');
INSERT OR REPLACE INTO cataloguenr VALUES ('IC1848', 'Collinder', '32');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-157', 'LBN', '537');
INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-157', 'Sh1', '109');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC7635', 'Sh2', '162');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC7822', 'Sh2', '171');

INSERT OR REPLACE INTO cataloguenr VALUES ('Ced214', 'LBN', '581');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC7023', 'LBN', '487');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC7129', 'LBN', '497');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC7380', 'LBN', '511');

INSERT OR REPLACE INTO cataloguenr VALUES ('Ced90', 'LBN', '1039');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC468', 'LBN', '1040');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC2359', 'Gum', '4');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC2359', 'RCW', '5');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC2359', 'Sh2', '298');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-91', 'LBN', '147');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6847', 'Sh2', '97');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-101', 'LBN', '168');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC1310', 'LBN', '181');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-104', 'LBN', '195');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6914', 'LBN', '280');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-115', 'LBN', '357');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC5070', 'LBN', '350');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC7000', 'Sh2', '117');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC1909', 'LBN', '959');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC2169', 'LBN', '903');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC446', 'LBN', '898');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC2264', 'LBN', '911');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC2264', 'Sh2', '273');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC466', 'Sh2', '288');
INSERT OR REPLACE INTO cataloguenr VALUES ('IC466', 'LBN', '1013');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-294', 'RCW', '3');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC1976', 'LBN', '974');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC1980', 'LBN', '977');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC2023', 'vdB', '52');

INSERT OR REPLACE INTO cataloguenr VALUES ('Ced62', 'LBN', '855');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC2175', 'LBN', '854');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC2175', 'Sh2', '252');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC2162', 'LBN', '859');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC348', 'LBN', '758');
INSERT OR REPLACE INTO cataloguenr VALUES ('IC348', 'Collinder', '41');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC1491', 'Sh2', '206');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC1499', 'Sh2', '220');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC1579', 'Sh2', '222');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-302', 'LBN', '1046');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-307', 'LBN', '1051');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-311', 'RCW', '16');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC2467', 'Sh2', '311');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-312', 'LBN', '1077');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-9', 'LBN', '1101');
INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-9', 'Gum', '65');
INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-9', 'vDB', '104');

INSERT OR REPLACE INTO cataloguenr VALUES ('vdB107', 'LBN', '1108');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6611', 'LBN', '67');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6611', 'IC', '4703');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-82', 'LBN', '129');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-84', 'LBN', '131');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-16', 'LBN', '1124');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6514', 'LBN', '27');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6514', 'Collinder', '360');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC4684', 'LBN', '34');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC1274', 'LBN', '33');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC4701', 'LBN', '55');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6589', 'LBN', '43');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6590', 'vdB', '119');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6590', 'Sh2', '37');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6618', 'LBN', '60');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC1952', 'LBN', '833');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-240', 'LBN', '822');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6842', 'LBN', '149');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6842', 'Sh1', '72');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC6842', 'Sh2', '95');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC4954', 'LBN', '153');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-223', 'LBN', '768');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC410', 'LBN', '807');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC417', 'Sh2', '234');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC1931', 'LBN', '810');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC1931', 'Sh2', '237');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-205', 'LBN', '701');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC10', 'LBN', '591');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC281', 'LBN', '616');
INSERT OR REPLACE INTO cataloguenr VALUES ('NGC281', 'Sh2', '184');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC1805', 'LBN', '654');
INSERT OR REPLACE INTO cataloguenr VALUES ('IC1805', 'Sh2', '190');
INSERT OR REPLACE INTO cataloguenr VALUES ('IC1805', 'Collinder', '26');

INSERT OR REPLACE INTO cataloguenr VALUES ('IC1848', 'LBN', '667');
INSERT OR REPLACE INTO cataloguenr VALUES ('IC1848', 'Sh2', '199');
INSERT OR REPLACE INTO cataloguenr VALUES ('IC1848', 'Collinder', '32');

INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-157', 'LBN', '537');
INSERT OR REPLACE INTO cataloguenr VALUES ('Sh2-157', 'Sh1', '109');

INSERT OR REPLACE INTO cataloguenr VALUES ('NGC7635', 'Sh2', '162');

PRAGMA user_version = 7;