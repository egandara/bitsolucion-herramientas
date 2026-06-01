# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_018_Base_Normativa_Operaciones_Actualizacion_Duplicados

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC **************************************************************************
# MAGIC * Nombre: BCI_018_Base_Normativa_Operaciones_Actualizacion_Duplicados.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor:  BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de registrar información de Operaciones Activas - Contingentes desde d00 Segmentado Prima
# MAGIC * Documentación: 
# MAGIC ***************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Mantenciones
# MAGIC **************************************************************************
# MAGIC #### Mantención Nro: 
# MAGIC * Autor: <Nombre Autor> (<Empresa del Autor (Bci/Otra)>) - Ing. SW BCI: <Nombre Ing. SW BCI>
# MAGIC * Fecha: <dd/mm/yyyy> 
# MAGIC * Descripción: <Descripción de la mantención>      
# MAGIC ************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tablas Entrada y Salida
# MAGIC **************************************************************************
# MAGIC #### Tabla Entrada ADLS: 
# MAGIC * base_archivos_normativos
# MAGIC ***************************************************************************
# MAGIC #### Tabla Salida Temporal: 
# MAGIC * tmp_

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variables

# COMMAND ----------

# MAGIC %md
# MAGIC ### Crear Widgets para Captura de Variables

# COMMAND ----------

dbutils.widgets.removeAll()
dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso :")
dbutils.widgets.text("db_platinumW","","02 platinum DB:")
dbutils.widgets.text("db_plat_tempW","","03 platinum temp db:")
dbutils.widgets.text("db_location_plat_tempW","","04 Location platinum temp db:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

db_platinumX = dbutils.widgets.get("db_platinumW")
spark.conf.set("bci.db_platinumX", db_platinumX)

db_plat_tempX = dbutils.widgets.get("db_plat_tempW")
spark.conf.set("bci.db_plat_tempX", db_plat_tempX)

db_location_plat_tempX = dbutils.widgets.get("db_location_plat_tempW")
spark.conf.set("bci.db_location_plat_tempX", db_location_plat_tempX)

print("fechaProcesoX: " + fechaProcesoX)
print("db_platinumX: " + db_platinumX)
print("db_plat_tempX: " + db_plat_tempX)
print("db_location_plat_tempX: " + db_location_plat_tempX)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Carga de funciones

# COMMAND ----------

# MAGIC %run "../../Funciones"

# COMMAND ----------

# MAGIC %md
# MAGIC ## Validación de ingreso de parámetros

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Parametro Vacio

# COMMAND ----------

valida_param_vacio(fechaProcesoX,'fechaProcesoX')
valida_param_vacio(db_platinumX,'db_platinumX')
valida_param_vacio(db_plat_tempX,'db_plat_tempX')
valida_param_vacio(db_location_plat_tempX,'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Base de Datos

# COMMAND ----------

valida_bd(db_platinumX, 'db_platinumX')
valida_bd(db_plat_tempX, 'db_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Fecha Valida

# COMMAND ----------

valida_fecha_valida(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Fecha Futura

# COMMAND ----------

valida_fecha_futura(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Ruta 

# COMMAND ----------

valida_ruta(db_location_plat_tempX, 'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ## Asigan Variables de fecha

# COMMAND ----------

ano = str(fechaProcesoX)[:4]
mes = str(fechaProcesoX)[4:][:2]
dia = str(fechaProcesoX)[6:][:2]
fechanormativo = str(ano+'-'+mes+'-'+dia)
fechacinta = str(dia+'-'+mes+'-'+ano)
anomes = str(ano+mes)
anomesdia = str(ano+mes+dia)

print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Inicio Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ## Creacion tablas temporales

# COMMAND ----------

# MAGIC %md
# MAGIC ###Tabla tmp_bci_18_duplicado

# COMMAND ----------

paso_tb_del_tmp_bci_18_duplicado  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_bci_18_duplicado"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_bci_18_duplicado)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_bci_18_duplicado", True)

# COMMAND ----------

paso_tb_crea_tmp_bci_18_duplicado  = """CREATE TABLE """ + db_plat_tempX + """.tmp_bci_18_duplicado
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  cod_num_operacion STRING COMMENT 'Número Operación',
  r_unico STRING COMMENT 'LLave formada por los campos: cod_num_operacion,rut_cliente, dv_cliente y cod_cartera_ope',
	cant_registro INT COMMENT 'Cantidad de Registros'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_bci_18_duplicado' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_bci_18_duplicado)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Tabla tmp_bci_18_fl_ope_duplicado

# COMMAND ----------

paso_tb_del_tmp_bci_18_flag_duplicado  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_bci_18_fl_ope_duplicado"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_bci_18_flag_duplicado)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_bci_18_fl_ope_duplicado", True)

# COMMAND ----------

paso_tb_crea_tmp_bci_18_flag_duplicado  = """CREATE TABLE """ + db_plat_tempX + """.tmp_bci_18_fl_ope_duplicado
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  cod_num_operacion STRING COMMENT 'Número Operación',
	fl_duplicado INT COMMENT 'Flag Operacion Duplicada'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_bci_18_fl_ope_duplicado' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_bci_18_flag_duplicado)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Tabla tmp_bci_18_segmento

# COMMAND ----------

paso_tb_del_tmp_bci_18_segmento  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_bci_18_segmento"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_bci_18_segmento)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_bci_18_segmento", True)

# COMMAND ----------

paso_tb_crea_tmp_bci_18_segmento  = """CREATE TABLE """ + db_plat_tempX + """.tmp_bci_18_segmento
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  cod_num_operacion STRING COMMENT 'Número Operación',
	mnt_deuda DECIMAL(30,0) COMMENT 'Monto Maximo de deuda por Operacion',
  mnt_provision DECIMAL(30,0) COMMENT 'Total de Provisiones por Operacion',
  mnt_min_val DECIMAL(30,0) COMMENT 'Monto Maximo de provision por Operacion'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_bci_18_segmento' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_bci_18_segmento)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Tabla tmp_bci_18_segmento_resp

# COMMAND ----------

paso_tb_del_tmp_bci_18_segmento2  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_bci_18_segmento_resp"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_bci_18_segmento2)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_bci_18_segmento_resp", True)

# COMMAND ----------

paso_tb_crea_tmp_bci_18_segmento2 = """CREATE TABLE """ + db_plat_tempX + """.tmp_bci_18_segmento_resp
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  cod_num_operacion STRING COMMENT 'Número Operación',
	mnt_deuda DECIMAL(30,0) COMMENT 'Monto Maximo de deuda por Operacion',
  mnt_provision DECIMAL(30,0) COMMENT 'Total de Provisiones por Operacion',
  mnt_min_val DECIMAL(30,0) COMMENT 'Monto Maximo de provision por Operacion'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_bci_18_segmento_resp' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_bci_18_segmento2)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla tmp_operacion_duplicada_v3

# COMMAND ----------

paso_base_normativa_duplicada_drop = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_operacion_duplicada_v3 """

# COMMAND ----------

sql_safe(paso_base_normativa_duplicada_drop)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_operacion_duplicada_v3", True)

# COMMAND ----------

paso_base_normativa_duplicada_create = """CREATE TABLE IF NOT EXISTS """ + db_plat_tempX + """.tmp_operacion_duplicada_v3
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente INT COMMENT 'RUT del Cliente Titular',
  dv_cliente STRING COMMENT 'Digito verificador del cliente titular',
	cod_ope_original STRING COMMENT 'Operación Original (TRZ)',
  cod_num_operacion STRING COMMENT 'Número Operación',
  cod_tipo_activo_cartera STRING COMMENT 'Tipo Activo ACT/CTG ',
	cod_tipo_ope STRING COMMENT 'Tipo-subtipo del D00',
	des_producto_ope STRING COMMENT  'Descripción Tipo de Cartera',
  des_banca_ope STRING COMMENT 'Descripción operacion banca',
	cod_cartera_ope STRING COMMENT 'Subtipo cartera',
	mnt_deuda_ope	   DECIMAL(32,0) COMMENT 'Saldo total ifrs',
  num_dias_mora_ope INT COMMENT 'Dias de mora',
  ind_cdet STRING COMMENT 'Indicador cartera deterioro',
  fec_ingreso_deterioro_ope DATE COMMENT 'Fecha ingreso de deterioro de la operación',
  cod_cctb STRING COMMENT 'Cuenta contable',
  fl_ope_reneg STRING COMMENT 'Flag de cartera renegociada',
  cod_tabla_34  INT COMMENT 'Tipo Activo Créditos de consumo y de vivienda Tabla CMF 34',
  cod_tabla_89  INT COMMENT 'Tipos de activos y créditos contingentes Tabla CMF 89',  
	nom_activo STRING COMMENT 'Nombre Activo Cartera',
	pct_pi DECIMAL(12,6) COMMENT 'PI',
  pct_pdi DECIMAL(12,6) COMMENT 'PDI',
  pct_pe DECIMAL(12,6) COMMENT 'PE',
	mnt_provision DECIMAL(32,0) COMMENT 'Prov Oficial',
	cod_calif STRING COMMENT 'Calificacion Cliente GR/A1/B1/C4',
	mnt_exposicion DECIMAL(32,0) COMMENT 'Prov Oficial',
	factor_expo DECIMAL(12,6) COMMENT 'PI',
	des_tipo_gtia STRING COMMENT 'Tipo garantia',		
	mnt_garantia DECIMAL(32,0) COMMENT 'MONTO_GARANTIA' ,
	pct_ltv DECIMAL(12,6) COMMENT 'PTVG',
	pct_pi_metodo_interno DECIMAL(12,6) COMMENT 'PI metodo interno',
  pct_pdi_metodo_interno DECIMAL(12,6) COMMENT 'PDI metodo interno',
  pct_pe_metodo_interno DECIMAL(12,6) COMMENT 'PE metodo interno',
	mnt_prov_metodo_interno  DECIMAL(32,0) COMMENT 'Provisión de la Colocación',
	mnt_avalado DECIMAL(32,0) COMMENT 'MONTO_AVALADO',
 fec_proceso DATE COMMENT 'Fecha de cierre' 
) 
USING DELTA
PARTITIONED BY(fec_proceso)
COMMENT 'Tabla que permite almacenar información del modelo normativo V3
Actualizacion: Mensual Incremental'
LOCATION '""" + db_location_plat_tempX + """tmp_operacion_duplicada_v3'  """

# COMMAND ----------

sql_safe(paso_base_normativa_duplicada_create)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Desarrollo Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 1: Update Operaciones Originales de TDC

# COMMAND ----------

# MAGIC %md
# MAGIC En Normativo Tarjetas BCI se deben dejar como **E0** (Ya Que No Existen Las E1) para que sean iguales a C12 y C20 y Tarjetas Nova se deben dejar como **W0**

# COMMAND ----------

query_upd_tdc_cod_orginal = """
update """+db_platinumX+""".base_archivos_normativos
  set cod_ope_original =  CASE WHEN SUBSTRING(trim(cod_ope_original),1,1) = 'E' THEN 'E0' || SUBSTRING(trim(cod_ope_original),3,10) 
                               WHEN SUBSTRING(trim(cod_ope_original),1,1) = 'W' THEN 'W0' || SUBSTRING(trim(cod_ope_original),3,10) 
                            ELSE trim(cod_ope_original) 
                          END
WHERE periodo_id = """+anomes+"""
		AND SUBSTRING(trim(cod_ope_original),1,2) in ('E1', 'W1')
"""

# COMMAND ----------

sql_safe(query_upd_tdc_cod_orginal )

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 2: NOVA TDC Consumo con problemas (no son W y deberian serlo)

# COMMAND ----------

# MAGIC %md
# MAGIC Operaciones asociadas a Cuenta Contable 141109514, correponden a TDC Nova

# COMMAND ----------

query_upd_tdc_nova_error = """
update """+db_platinumX+""".base_archivos_normativos
  set cod_ope_original =  CASE WHEN SUBSTRING(trim(cod_ope_original),1,1) NOT IN ('E','W') THEN 'W0' || SUBSTRING(trim(cod_ope_original),3,10) 
                            ELSE trim(cod_ope_original) 
                          END
WHERE periodo_id = """+anomes+"""
		AND TRIM(cod_cctb) = '141109514' 
		AND SUBSTRING(trim(cod_ope_original),1,1) NOT IN ('E', 'W')
"""

# COMMAND ----------

sql_safe(query_upd_tdc_nova_error)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 3: Update Operaciones MACH Originales

# COMMAND ----------

# MAGIC %md
# MAGIC Se agrega lógica MACH para cambiar cod_ope_original de M91 y M92

# COMMAND ----------

query_upd_mach_cod_orginal = """
update """+db_platinumX+""".base_archivos_normativos
  set cod_ope_original =  CASE WHEN SUBSTRING(trim(cod_ope_original),1,3) in ('M91', 'M92') THEN 'M90' || SUBSTRING(trim(cod_ope_original),4,9)  
		ELSE trim(cod_ope_original) 
	END
WHERE periodo_id = """+anomes+"""
		AND SUBSTRING(trim(cod_ope_original),1,3) in ('M91', 'M92')
"""

# COMMAND ----------

sql_safe(query_upd_mach_cod_orginal)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 4: Actualizacion Traza NOVA Comercial Y Consumo

# COMMAND ----------

query_upd_trz_nova_cons = """
update """+db_platinumX+""".base_archivos_normativos
  set cod_ope_original = cod_num_operacion
where periodo_id = """+anomes+"""
    and regexp_instr(cod_num_operacion, '[0,9]') = 1
    and regexp_instr(upper(cod_ope_original), 'D') = 1 """

# COMMAND ----------

sql_safe(query_upd_trz_nova_cons)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 5: Actualizacion provision=1 donde prov = 0 y deuda < 30.000

# COMMAND ----------

query_upd_sin_provision = """
update """+db_platinumX+""".base_archivos_normativos
  set mnt_provision =  1
WHERE periodo_id = """+anomes+"""
		AND  (mnt_deuda_ope > 0 and mnt_deuda_ope < 30000 )
    AND mnt_provision < 1
"""
 

# COMMAND ----------

sql_safe(query_upd_sin_provision)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 6: Actualizacion Provision Operaciones Individuales Que No Poseen Metodo Interno Para C50

# COMMAND ----------

query_upd_sin_provision_mi = """
update """+db_platinumX+""".base_archivos_normativos
  set mnt_prov_metodo_interno = mnt_provision 
WHERE periodo_id = """+anomes+"""
		AND trim(cod_calif) <> 'GR'
  and trim(cod_cctb) in ('120501603','120501606','111001500','111001601','120500031','111001608','120511601','120501605','120511604','120501601','120511603','111001609','120501600','120511600','111001607','140110016','140100029','120501604')
"""
 

# COMMAND ----------

sql_safe(query_upd_sin_provision_mi)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 7: Obtencion Operaciones Duplicadas

# COMMAND ----------

query_ope_duplicados = """ insert into """ + db_plat_tempX + """.tmp_bci_18_duplicado
select  periodo_id
    ,cod_num_operacion                                                as cod_num_operacion
    ,concat(trim(cod_num_operacion),rut_cliente, trim(dv_cliente), trim(cod_cartera_ope))  as r_unico
    ,count(1)                                                           as cantidad
from """+db_platinumX+""".base_archivos_normativos
where periodo_id = """+anomes+"""
GROUP BY periodo_id, cod_num_operacion, concat(trim(cod_num_operacion),rut_cliente, trim(dv_cliente), trim(cod_cartera_ope))
HAVING count(1) > 1
"""

# COMMAND ----------

sql_safe(query_ope_duplicados )

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 8: Deteccion si el caso efectivamente esta duplicado o se deben sumar los registros (Caso individual)

# COMMAND ----------

query_flag_ope_duplicados = """ insert into """ + db_plat_tempX + """.tmp_bci_18_fl_ope_duplicado
select periodo_id
    ,cod_num_operacion
    ,case when min(mnt_provision) = max(mnt_provision) then 0
            else 1 end as flag_duplicado
from """+db_platinumX+""".base_archivos_normativos
where periodo_id = """+anomes+"""
and trim(cod_num_operacion) in (select distinct cod_num_operacion from """ + db_plat_tempX + """.tmp_bci_18_duplicado where periodo_id = """+anomes+""" )
group by periodo_id, cod_num_operacion
"""

# COMMAND ----------

sql_safe(query_flag_ope_duplicados)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 9: Se Borran las operaciones duplicadas con misma provision y se agrega un solo un registro de la Operacion

# COMMAND ----------

# MAGIC %md
# MAGIC ####Se eliminan registros de tabla temporal de duplicados

# COMMAND ----------

query_dlt_v3_tmp_dup = """DELETE FROM  """+db_plat_tempX+""".tmp_operacion_duplicada_v3 where periodo_id = """+anomes+""" """

# COMMAND ----------

sql_safe(query_dlt_v3_tmp_dup )

# COMMAND ----------

# MAGIC %md
# MAGIC #### Se crea registro unico para duplicados

# COMMAND ----------

query_ins_carga_duplicados = """ insert into """+db_plat_tempX+""".tmp_operacion_duplicada_v3 
select distinct * 
from """+db_platinumX+""".base_archivos_normativos
where periodo_id = """+anomes+"""
and trim(cod_num_operacion) in (select distinct trim(cod_num_operacion) as cod_num_operacion from """ + db_plat_tempX + """.tmp_bci_18_fl_ope_duplicado where periodo_id = """+anomes+""" and fl_duplicado = 0 )
"""

# COMMAND ----------

sql_safe(query_ins_carga_duplicados)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Se Agrupa por valores maximos de deuda y suma de provisión para las operaciones que se encuentran segmentadas

# COMMAND ----------

qry_vw_segmentado = """ create or replace temporary view vw_tmp_segmentados as
select periodo_id  
    ,trim(cod_num_operacion)  as cod_num_operacion
    ,mnt_deuda_ope            as mnt_deuda
    ,mnt_provision            as mnt_provision
--into #tmp_segmentados          
from """+db_platinumX+""".base_archivos_normativos
where periodo_id = """+anomes+"""
and trim(cod_num_operacion) in (select distinct trim(cod_num_operacion) cod_num_operacion from """ + db_plat_tempX + """.tmp_bci_18_fl_ope_duplicado where periodo_id = """+anomes+""" and fl_duplicado = 1 )
--group by periodo_id, cod_num_operacion,  mnt_provision
"""

# COMMAND ----------

sql_safe(qry_vw_segmentado)

# COMMAND ----------

qry_vw_segmentado_2 = """ create or replace temporary view vw_tmp_segmentados_2 as
select periodo_id  
    ,trim(cod_num_operacion)  as cod_num_operacion
    ,max(mnt_deuda)           as mnt_deuda   
from vw_tmp_segmentados
where periodo_id = """+anomes+"""
group by periodo_id
  , trim(cod_num_operacion)
"""

# COMMAND ----------

sql_safe(qry_vw_segmentado_2)

# COMMAND ----------

qry_vw_segmentado_3 = """ create or replace temporary view vw_tmp_segmentados_3 as
select a.periodo_id  
    ,trim(a.cod_num_operacion)  as cod_num_operacion
    ,b.mnt_deuda                as mnt_deuda
    ,a.mnt_provision            as mnt_provision 
from vw_tmp_segmentados a 
inner join vw_tmp_segmentados_2 b 
  on a.periodo_id = b.periodo_id
  and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
where a.periodo_id = """+anomes+""" """ 

# COMMAND ----------

sql_safe(qry_vw_segmentado_3)

# COMMAND ----------

qry_vw_segmentado_4 = """ create or replace temporary view vw_tmp_segmentados_4 as
select periodo_id  
    ,trim(cod_num_operacion)      as cod_num_operacion
    ,min(mnt_provision)           as mnt_min_provision     
from vw_tmp_segmentados
where periodo_id = """+anomes+"""
group by periodo_id
  ,trim(cod_num_operacion) 

"""

# COMMAND ----------

sql_safe(qry_vw_segmentado_4)

# COMMAND ----------

qry_vw_segmentado_5 = """ create or replace temporary view vw_tmp_segmentados_5 as
select a.periodo_id  
    ,trim(a.cod_num_operacion)  as cod_num_operacion
    ,a.mnt_deuda                as mnt_deuda
    ,a.mnt_provision            as mnt_provision 
    ,b.mnt_min_provision        as mnt_min_provision
from vw_tmp_segmentados_3 a 
inner join vw_tmp_segmentados_4 b 
  on a.periodo_id = b.periodo_id
  and trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
where a.periodo_id = """+anomes+""" """ 

# COMMAND ----------

sql_safe(qry_vw_segmentado_5)

# COMMAND ----------

qry_ins_segmento = """ insert into """ + db_plat_tempX + """.tmp_bci_18_segmento
select periodo_id  
    ,trim(cod_num_operacion)    as cod_num_operacion
    ,mnt_deuda                  as deuda
    ,sum(mnt_provision)         as provision
    ,mnt_min_provision          as min_val
--into #tmp_segmentados          
from vw_tmp_segmentados_5
group by periodo_id, trim(cod_num_operacion), mnt_deuda, mnt_min_provision
"""

# COMMAND ----------

sql_safe(qry_ins_segmento)

# COMMAND ----------

qry_ins_segmento_2 = """ insert into  """ + db_plat_tempX + """.tmp_bci_18_segmento_resp
select periodo_id  
    ,cod_num_operacion
    ,max(mnt_deuda) as deuda
    ,mnt_provision
    ,mnt_min_val
from """ + db_plat_tempX + """.tmp_bci_18_segmento
where periodo_id = """+anomes+"""
group by  periodo_id  
    ,cod_num_operacion
    ,mnt_provision
    ,mnt_min_val
"""   

# COMMAND ----------

# MAGIC %md
# MAGIC sql_safe(qry_ins_segmento_2)

# COMMAND ----------

# MAGIC %md
# MAGIC qry_dlt_tbl_segmento = """ DELETE from """ + db_plat_tempX + """.tmp_bci_18_segmento
# MAGIC where periodo_id = """+anomes+""" """
# MAGIC sql_safe(qry_dlt_tbl_segmento)

# COMMAND ----------

qry_ins_segmento_3 = """ insert into  """ + db_plat_tempX + """.tmp_bci_18_segmento
select periodo_id  
    ,cod_num_operacion
    ,mnt_deuda
    ,mnt_provision
    ,min(mnt_min_val) as prov_minima
from """ + db_plat_tempX + """.tmp_bci_18_segmento_resp
where periodo_id = """+anomes+"""
group by  periodo_id  
    ,cod_num_operacion
    ,mnt_deuda
    ,mnt_provision
"""   

# COMMAND ----------

# MAGIC %md
# MAGIC sql_safe(qry_ins_segmento_3)

# COMMAND ----------

# MAGIC %md
# MAGIC qry_dlt_tbl_segmento_resp = """ DELETE from """ + db_plat_tempX + """.tmp_bci_18_segmento_resp
# MAGIC where periodo_id = """+anomes+""" """
# MAGIC
# MAGIC sql_safe(qry_dlt_tbl_segmento_resp)

# COMMAND ----------

# MAGIC %md
# MAGIC #### Se Insertan Operaciones en tabla temporal para actualizar los montos de provisión

# COMMAND ----------

qry_insert_duplicado_v3 = """ insert into """+db_plat_tempX+""".tmp_operacion_duplicada_v3
select distinct a.periodo_id
  ,a.rut_cliente
  ,a.dv_cliente
	,a.cod_ope_original
  ,a.cod_num_operacion
  ,a.cod_tip_cart
	,a.cod_tipo_ope
	,a.des_producto_ope
  ,a.des_banca_ope
	,a.cod_cartera_ope
	,b.mnt_deuda as mnt_deuda_ope
  ,a.num_dias_mora_ope
  ,a.ind_cdet
  ,a.fec_ingreso_deterioro_ope 
  ,a.cod_cctb
  ,a.fl_ope_reneg
  ,a.cod_tabla_34
  ,a.cod_tabla_89
	,a.nom_activo
	,a.pct_pi
  ,a.pct_pdi
  ,a.pct_pe
	,b.mnt_provision
	,a.cod_calif
	,a.mnt_exposicion
	,a.factor_expo
	,a.des_tipo_gtia
	,a.mnt_garantia
	,a.pct_ltv
	,a.pct_pi_metodo_interno
  ,a.pct_pdi_metodo_interno
  ,a.pct_pe_metodo_interno
	,a.mnt_prov_metodo_interno
	,a.mnt_avalado
  ,a.fec_proceso
from """+db_platinumX+""".base_archivos_normativos a
  inner join """ + db_plat_tempX + """.tmp_bci_18_segmento b
    on trim(a.cod_num_operacion) = trim(b.cod_num_operacion)
    and a.mnt_provision =  b.mnt_min_val
where a.periodo_id = """+anomes+"""
""" 

# COMMAND ----------

sql_safe(qry_insert_duplicado_v3)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 10: Eliminacion de registros duplicados de mismo valor de provisión en Base Normativa para Fecha de Proceso

# COMMAND ----------

query_dlt_v3_norm_dup = """DELETE FROM """+db_platinumX+""".base_archivos_normativos WHERE periodo_id = """+anomes+""" and cod_num_operacion in (select distinct cod_num_operacion from """+db_plat_tempX+""".tmp_operacion_duplicada_v3 where periodo_id = """+anomes+""") """

# COMMAND ----------

sql_safe(query_dlt_v3_norm_dup)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 11: Inserta registro unico para operaciones duplicadas Eliminadas en Base Normativa para Fecha de Proceso

# COMMAND ----------

query_ins_v3_normativo = """ insert into """+db_platinumX+""".base_archivos_normativos
select periodo_id
	,rut_cliente
	,dv_cliente
	,cod_ope_original
	,cod_num_operacion
	,cod_tipo_activo_cartera
	,cod_tipo_ope
	,des_producto_ope
	,des_banca_ope
	,cod_cartera_ope
	,mnt_deuda_ope
	,num_dias_mora_ope
	,ind_cdet
	,fec_ingreso_deterioro_ope 
	,cod_cctb
	,fl_ope_reneg
	,-998                              as cod_tabla_34
  ,cod_tabla_89
	,nom_activo
	,pct_pi
	,pct_pdi
	,pct_pe
	,mnt_provision
	,cod_calif
	,mnt_exposicion
	,factor_expo
	,des_tipo_gtia
	,mnt_garantia
	,pct_ltv
	,pct_pi_metodo_interno
	,pct_pdi_metodo_interno
	,pct_pe_metodo_interno
	,mnt_prov_metodo_interno
	,mnt_avalado
	,fec_proceso
from """+db_plat_tempX+""".tmp_operacion_duplicada_v3
where periodo_id = """+anomes+""" """

# COMMAND ----------

sql_safe(query_ins_v3_normativo)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Conteo de registros insertados en tabla de salida

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """+db_platinumX+""".base_archivos_normativos where periodo_id = """+anomes+""" """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla final base_archivos_normativos ")

# COMMAND ----------

# MAGIC %md
# MAGIC ## Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")